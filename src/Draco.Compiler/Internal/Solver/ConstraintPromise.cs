using System;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Factory methods for <see cref="IConstraintPromise"/>s.
/// </summary>
internal static class ConstraintPromise
{
    /// <summary>
    /// Constructs a constraint promise that is already resolved.
    /// </summary>
    /// <typeparam name="TResult">The result type of the promise.</typeparam>
    /// <param name="result">The resolved value.</param>
    /// <returns>The constructed promise, containing <paramref name="result"/> as the result value.</returns>
    public static IConstraintPromise<TResult> FromResult<TResult>(TResult result) =>
        // TODO
        throw new NotImplementedException();

    /// <summary>
    /// Maps the result of the given constraint promise using a mapping function.
    /// </summary>
    /// <typeparam name="TOldResult">The result the promise originally returned.</typeparam>
    /// <typeparam name="TNewResult">The type the mapping function returns.</typeparam>
    /// <param name="promise">The promise to map.</param>
    /// <param name="func">The mapping function.</param>
    /// <returns>A new promise, that maps the result of <paramref name="promise"/> using <paramref name="func"/>.</returns>
    public static IConstraintPromise<TNewResult> Map<TOldResult, TNewResult>(
        this IConstraintPromise<TOldResult> promise,
        Func<TOldResult, TNewResult> func) =>
        // TODO
        throw new NotImplementedException();

    /// <summary>
    /// Binds a constraint promise to a new promise based on the old result, once the promise resolves.
    /// </summary>
    /// <typeparam name="TOldResult">The result of the old promise.</typeparam>
    /// <typeparam name="TNewResult">The result the mapped promise returns.</typeparam>
    /// <param name="promise">The constraint promise to bind.</param>
    /// <param name="func">A function transforming the result of <paramref name="promise"/> to the new constraint promise.</param>
    /// <returns>A new constraint promise, that only gets created, when <paramref name="promise"/> is resolved.</returns>
    public static IConstraintPromise<TNewResult> Bind<TOldResult, TNewResult>(
        this IConstraintPromise<TOldResult> promise,
        Func<TOldResult, IConstraintPromise<TNewResult>> func) =>
        // TODO
        throw new NotImplementedException();
}
