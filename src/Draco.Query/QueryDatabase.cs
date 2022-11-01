using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Query;

/// <summary>
/// The type that manages the memoization and garbage collection of query results.
/// </summary>
public static class QueryDatabase
{
    /// <summary>
    /// Called, when a query with new keys are called.
    /// </summary>
    /// <typeparam name="TResult">The result type of the query.</typeparam>
    /// <param name="identifier">The identifier of the query.</param>
    internal static void OnNewQuery<TResult>(QueryIdentifier identifier) =>
        throw new NotImplementedException();

    /// <summary>
    /// Called, when a query has finished its computation.
    /// </summary>
    /// <typeparam name="TResult">The result type of the query.</typeparam>
    /// <param name="identifier">The identifier of the query.</param>
    /// <param name="result">The computed result of the query.</param>
    internal static void OnQueryResult<TResult>(QueryIdentifier identifier, TResult result) =>
        throw new NotImplementedException();

    /// <summary>
    /// Called, when a dependency is discovered between two queries.
    /// </summary>
    /// <param name="dependent">The identifier of the query that is dependent on <paramref name="dependency"/>.</param>
    /// <param name="dependency">The query that is called by <paramref name="dependent"/>.</param>
    internal static void OnDependency(QueryIdentifier dependent, QueryIdentifier dependency) =>
        throw new NotImplementedException();

    /// <summary>
    /// Attempts to retrieve the up to date result of a query.
    /// </summary>
    /// <typeparam name="TResult">The result type of the query.</typeparam>
    /// <param name="identifier">The query identifier.</param>
    /// <param name="result">The retrieved result, if it's up to date.</param>
    /// <returns>True, if the query named <paramref name="identifier"/> has an up to date result and the result
    /// is written to <paramref name="result"/>.</returns>
    internal static bool TryGetUpToDateResult<TResult>(
        QueryIdentifier identifier,
        [MaybeNullWhen(false)] out TResult result) =>
        throw new NotImplementedException();
}
