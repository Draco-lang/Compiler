using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Draco.Query.Tasks;

namespace Draco.Query;

// TODO: Thread-safety

/// <summary>
/// The type that manages the memoization and garbage collection of query results.
/// </summary>
public sealed class QueryDatabase
{
    /// <summary>
    /// The interface of computed query results.
    /// </summary>
    private interface IResult
    {
        /// <summary>
        /// The revision where the result has last changed.
        /// </summary>
        public Revision ChangedAt { get; }

        /// <summary>
        /// The revision where the result has last been verified to be reusable.
        /// </summary>
        public Revision VerifiedAt { get; }

        /// <summary>
        /// The dependencies of this result.
        /// </summary>
        public ICollection<IResult> Dependencies { get; }

        /// <summary>
        /// Refreshes this result.
        /// </summary>
        public Task Refresh();
    }

    /// <summary>
    /// Information about an input result.
    /// </summary>
    /// <typeparam name="T">The type of the input value.</typeparam>
    private sealed class InputResult<T> : IResult
    {
        public Revision ChangedAt { get; set; } = Revision.Invalid;
        public Revision VerifiedAt => Revision.MaxValue;
        public ICollection<IResult> Dependencies => Array.Empty<IResult>();
        public T Value { get; set; } = default!;

        public Task Refresh() => Task.CompletedTask;
    }

    /// <summary>
    /// Information about a computed result.
    /// </summary>
    /// <typeparam name="T">The type of the computed value.</typeparam>
    private sealed class ComputedResult<T> : IResult
    {
        public Revision ChangedAt { get; set; } = Revision.Invalid;
        public Revision VerifiedAt { get; set; } = Revision.Invalid;
        public ICollection<IResult> Dependencies { get; } = new List<IResult>();
        public T Value { get; set; } = default!;

        private readonly QueryIdentifier identifier;

        public ComputedResult(QueryIdentifier identifier)
        {
            this.identifier = identifier;
        }

        public async Task Refresh() =>
            await QueryValueTaskMethodBuilder<T>.RunQueryByIdentifier(this.identifier);
    }

    /// <summary>
    /// The current revision the system is at.
    /// </summary>
    public Revision CurrentRevision { get; private set; } = Revision.New;

    private readonly ConcurrentDictionary<QueryIdentifier, IResult> queries = new();

    /// <summary>
    /// Creates an input for the system.
    /// </summary>
    /// <typeparam name="TResult">The type of the input value.</typeparam>
    /// <returns>The identifier for the input.</returns>
    public QueryIdentifier CreateInput<TResult>()
    {
        var identifier = QueryIdentifier.New;
        this.queries.TryAdd(identifier, new InputResult<TResult>());
        return identifier;
    }

    /// <summary>
    /// Sets an input for the system.
    /// </summary>
    /// <typeparam name="TResult">The type of the input value.</typeparam>
    /// <param name="identifier">The identifier for the input.</param>
    /// <param name="value">The value to set the input to.</param>
    public void SetInput<TResult>(QueryIdentifier identifier, TResult value)
    {
        var cachedResult = (InputResult<TResult>)this.queries[identifier];
        this.CurrentRevision = Revision.New;
        cachedResult.Value = value;
        cachedResult.ChangedAt = this.CurrentRevision;
    }

    /// <summary>
    /// Retrieves an input from the system.
    /// </summary>
    /// <typeparam name="TResult">The type of the input value.</typeparam>
    /// <param name="identifier">The identifier for the input.</param>
    /// <returns>The retrieved input as a task.</returns>
    public QueryValueTask<TResult> GetInput<TResult>(QueryIdentifier identifier)
    {
        var cachedResult = (InputResult<TResult>)this.queries[identifier];
        return new(cachedResult.Value, identifier);
    }

    /// <summary>
    /// Called, when a query with new keys are called.
    /// </summary>
    /// <typeparam name="TResult">The result type of the query.</typeparam>
    /// <param name="identifier">The identifier of the query.</param>
    internal void OnNewQuery<TResult>(QueryIdentifier identifier) =>
        // Add an empty entry
        this.queries.TryAdd(identifier, new ComputedResult<TResult>(identifier));

    /// <summary>
    /// Called, when a query has finished its computation.
    /// </summary>
    /// <typeparam name="TResult">The result type of the query.</typeparam>
    /// <param name="identifier">The identifier of the query.</param>
    /// <param name="result">The computed result of the query.</param>
    internal void OnQueryResult<TResult>(QueryIdentifier identifier, TResult result)
    {
        // Refresh revision
        this.queries.AddOrUpdate(
            key: identifier,
            // NOTE: Should never happen
            addValueFactory: _ => throw new InvalidOperationException(),
            updateValueFactory: (_, cached) =>
            {
                var cachedResult = (ComputedResult<TResult>)cached;
                var changed = !Equals(result, cachedResult.Value);
                if (changed) cachedResult.ChangedAt = this.CurrentRevision;
                cachedResult.VerifiedAt = this.CurrentRevision;
                cachedResult.Value = result;
                return cachedResult;
            });
    }

    /// <summary>
    /// Called, when a dependency is discovered between two queries.
    /// </summary>
    /// <param name="dependent">The identifier of the query that is dependent on <paramref name="dependency"/>.</param>
    /// <param name="dependency">The query that is called by <paramref name="dependent"/>.</param>
    internal void OnQueryDependency(QueryIdentifier dependent, QueryIdentifier dependency)
    {
        var dependentResult = this.queries[dependent];
        var dependencyResult = this.queries[dependency];
        dependentResult.Dependencies.Add(dependencyResult);
    }

    /// <summary>
    /// Attempts to retrieve the up to date result of a query.
    /// </summary>
    /// <typeparam name="TResult">The result type of the query.</typeparam>
    /// <param name="identifier">The query identifier.</param>
    /// <param name="result">The retrieved result, if it's up to date.</param>
    /// <returns>True, if the query named <paramref name="identifier"/> has an up to date result and the result
    /// is written to <paramref name="result"/>.</returns>
    internal bool TryGetUpToDateQueryResult<TResult>(
        QueryIdentifier identifier,
        [MaybeNullWhen(false)] out TResult result)
    {
        var cachedResult = (ComputedResult<TResult>)this.queries[identifier];
        // The value has never been memoized yet
        if (cachedResult.ChangedAt == Revision.Invalid)
        {
            // Force recomputation
            result = default;
            return false;
        }
        // Value is already memoized, but potentially outdated
        // If we have been verified to be valid already in the current version, we can just clone and return
        if (cachedResult.VerifiedAt == this.CurrentRevision)
        {
            result = cachedResult.Value!;
            return true;
        }
        // Check if the dependencies are up to date
        // TODO: This blocks synchronously but I have no idea if we can even make this method async
        Task.WaitAll(cachedResult.Dependencies.Select(dep => dep.Refresh()).ToArray());

        // Now check wether dependencies have been updated since this one
        if (cachedResult.Dependencies.All(dep => dep.ChangedAt <= cachedResult.VerifiedAt))
        {
            // All dependencies came from earlier revisions, they are safe to reuse
            // Which means this value is also safe to reuse, update verification number
            cachedResult.VerifiedAt = this.CurrentRevision;
            result = cachedResult.Value!;
            return true;
        }

        // Some values must have gone outdated and got recomputed, we also need to recompute, force recomputation
        result = default;
        return false;
    }
}
