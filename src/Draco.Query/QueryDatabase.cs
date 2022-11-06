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
/// The central database that manages memoization, caches results and does garbage collection.
/// This is also the main configuration point of the system.
/// </summary>
public sealed class QueryDatabase
{
    /// <summary>
    /// The current revision the system is at.
    /// </summary>
    public Revision CurrentRevision { get; private set; } = Revision.New;

    private readonly ConcurrentDictionary<object, IQueryResult> inputs = new();
    private readonly ConcurrentDictionary<QueryIdentifier, IQueryResult> queries = new();

    /// <summary>
    /// Retrieves an input from the system.
    /// </summary>
    /// <typeparam name="TResult">The type of the input value.</typeparam>
    /// <param name="key">The input value key.</param>
    /// <returns>The retrieved input as a task.</returns>
    public QueryValueTask<TResult> GetInput<TResult>(object key)
    {
        var result = (InputQueryResult<TResult>)this.inputs[key];
        return new(result.Value, result.Identifier);
    }

    /// <summary>
    /// Sets an input for the system.
    /// </summary>
    /// <typeparam name="TResult">The type of the input value.</typeparam>
    /// <param name="key">The input value key.</param>
    /// <param name="value">The value to set the input to.</param>
    public void SetInput<TResult>(object key, TResult value)
    {
        if (!this.inputs.TryGetValue(key, out var result))
        {
            // Input does not exist yet
            var identifier = QueryIdentifier.New;
            result = new InputQueryResult<TResult>(identifier);
            this.inputs[key] = result;
            this.queries[identifier] = result;
        }
        // Refresh revision and value
        this.CurrentRevision = Revision.New;
        var cachedResult = (InputQueryResult<TResult>)result;
        cachedResult.ChangedAt = this.CurrentRevision;
        cachedResult.Value = value;
    }

    /// <summary>
    /// Called, when a query with new keys are called.
    /// </summary>
    /// <typeparam name="TResult">The result type of the query.</typeparam>
    /// <param name="identifier">The identifier of the query.</param>
    internal void OnNewQuery<TResult>(QueryIdentifier identifier) =>
        // Add an empty entry
        this.queries.TryAdd(identifier, new ComputedQueryResult<TResult>(identifier));

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
                var cachedResult = (ComputedQueryResult<TResult>)cached;
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
        var cachedResult = (ComputedQueryResult<TResult>)this.queries[identifier];
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
