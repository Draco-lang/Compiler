using System;
using System.Collections.Generic;

namespace Draco.Fuzzing;

/// <summary>
/// A type mutating the input data in order to minimize it.
/// </summary>
/// <typeparam name="TInput">The type of the input data.</typeparam>
public interface IInputMinimizer<TInput>
{
    /// <summary>
    /// Produces a sequence of trimmed inputs without changing the coverage. Checking for that is the responsibility of the caller.
    /// </summary>
    /// <param name="random">The random number generator.</param>
    /// <param name="input">The input to minimize.</param>
    /// <returns>A sequence of minimized inputs, that hopefully results in the same coverage.</returns>
    public IEnumerable<TInput> Minimize(Random random, TInput input);
}

/// <summary>
/// Factory for common input minimization logic.
/// </summary>
public static class InputMinimizer
{
    /// <summary>
    /// Creates an input minimizer from the given function.
    /// </summary>
    /// <typeparam name="TInput">The type of the input data.</typeparam>
    /// <param name="func">The function to minimize the input.</param>
    /// <returns>The input minimizer.</returns>
    public static IInputMinimizer<TInput> Create<TInput>(Func<Random, TInput, IEnumerable<TInput>> func) =>
        new DelegateMinimizer<TInput>(func);

    private sealed class DelegateMinimizer<TInput>(Func<Random, TInput, IEnumerable<TInput>> func) : IInputMinimizer<TInput>
    {
        public IEnumerable<TInput> Minimize(Random random, TInput input) => func(random, input);
    }
}
