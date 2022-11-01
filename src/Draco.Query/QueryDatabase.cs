using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Query;

/// <summary>
/// The type that manages the memoization and garbage collection of query results.
/// </summary>
public static class QueryDatabase
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
        public IProducerConsumerCollection<IResult> Dependencies { get; }
    }

    /// <summary>
    /// Information about a computed result.
    /// </summary>
    /// <typeparam name="T">The type of the computed value.</typeparam>
    /// <param name="ChangedAt">See <see cref="IResult"/>.</param>
    /// <param name="VerifiedAt">See <see cref="IResult"/>.</param>
    /// <param name="Dependencies">See <see cref="IResult"/>.</param>
    /// <param name="Value">The computed, cached value.</param>
    private readonly record struct Result<T>(
        Revision ChangedAt,
        Revision VerifiedAt,
        ConcurrentBag<IResult> Dependencies,
        T? Value) : IResult
    {
        IProducerConsumerCollection<IResult> IResult.Dependencies => this.Dependencies;
    }

    /// <summary>
    /// The current revision the system is at.
    /// </summary>
    public static Revision CurrentRevision { get; } = Revision.New;

    private static readonly ConcurrentDictionary<QueryIdentifier, IResult> queries = new();

    /// <summary>
    /// Called, when a query with new keys are called.
    /// </summary>
    /// <typeparam name="TResult">The result type of the query.</typeparam>
    /// <param name="identifier">The identifier of the query.</param>
    internal static void OnNewQuery<TResult>(QueryIdentifier identifier) =>
        // Add an empty entry
        queries.TryAdd(identifier, new Result<TResult>(
            ChangedAt: Revision.Invalid,
            VerifiedAt: Revision.Invalid,
            Dependencies: new(),
            Value: default));

    /// <summary>
    /// Called, when a query has finished its computation.
    /// </summary>
    /// <typeparam name="TResult">The result type of the query.</typeparam>
    /// <param name="identifier">The identifier of the query.</param>
    /// <param name="result">The computed result of the query.</param>
    internal static void OnQueryResult<TResult>(QueryIdentifier identifier, TResult result)
    {
        // Refresh revision
        queries.AddOrUpdate(
            key: identifier,
            // NOTE: Should never happen
            addValueFactory: _ => throw new InvalidOperationException(),
            updateValueFactory: (_, cached) =>
            {
                var cachedResult = (Result<TResult>)cached;
                // TODO: We might need a lock for the revision reading here?
                return cachedResult with
                {
                    ChangedAt = CurrentRevision,
                    VerifiedAt = CurrentRevision,
                    Value = result,
                };
            });
    }

    /// <summary>
    /// Called, when a dependency is discovered between two queries.
    /// </summary>
    /// <param name="dependent">The identifier of the query that is dependent on <paramref name="dependency"/>.</param>
    /// <param name="dependency">The query that is called by <paramref name="dependent"/>.</param>
    internal static void OnQueryDependency(QueryIdentifier dependent, QueryIdentifier dependency)
    {
        // NOTE: Should never happen
        if (!queries.TryGetValue(dependent, out var dependentResult)) throw new InvalidOperationException();
        // NOTE: Should never happen
        if (!queries.TryGetValue(dependency, out var dependencyResult)) throw new InvalidOperationException();
        dependentResult.Dependencies.TryAdd(dependencyResult);
    }

    /// <summary>
    /// Attempts to retrieve the up to date result of a query.
    /// </summary>
    /// <typeparam name="TResult">The result type of the query.</typeparam>
    /// <param name="identifier">The query identifier.</param>
    /// <param name="result">The retrieved result, if it's up to date.</param>
    /// <returns>True, if the query named <paramref name="identifier"/> has an up to date result and the result
    /// is written to <paramref name="result"/>.</returns>
    internal static bool TryGetUpToDateQueryResult<TResult>(
        QueryIdentifier identifier,
        [MaybeNullWhen(false)] out TResult result)
    {
        // NOTE: Should never happen
        if (!queries.TryGetValue(identifier, out var cached)) throw new InvalidOperationException();
        var cachedResult = (Result<TResult>)cached;
        // The value has never been memoized yet
        if (cachedResult.ChangedAt == Revision.Invalid)
        {
            // Force recomputation
            result = default;
            return false;
        }
        // Value is already memoized, but potentially outdated
        // If we have been verified to be valid already in the current version, we can just clone and return
        if (cachedResult.VerifiedAt == CurrentRevision)
        {
            result = cachedResult.Value!;
            return true;
        }
        // TODO: Check on dependencies
        throw new NotImplementedException();
    }
}
