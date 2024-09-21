using System;
using System.Collections.Generic;

namespace Draco.Fuzzing;

/// <summary>
/// A type mutating the input data in order to discover new paths.
/// </summary>
/// <typeparam name="TInput">The type of the input data.</typeparam>
public interface IInputMutator<TInput>
{
    /// <summary>
    /// Produces a sequence of mutated inputs.
    /// </summary>
    /// <param name="random">The random number generator.</param>
    /// <param name="input">The input to mutate.</param>
    /// <returns>A sequence of mutated inputs.</returns>
    public IEnumerable<TInput> Mutate(Random random, TInput input);
}

/// <summary>
/// Factory for common input mutation logic.
/// </summary>
public static class InputMutator
{
    /// <summary>
    /// Creates an input mutator from the given function.
    /// </summary>
    /// <typeparam name="TInput">The type of the input data.</typeparam>
    /// <param name="func">The function to mutate the input.</param>
    /// <returns>The input mutator.</returns>
    public static IInputMutator<TInput> Create<TInput>(Func<Random, TInput, IEnumerable<TInput>> func) =>
        new DelegateMutator<TInput>(func);

    private sealed class DelegateMutator<TInput>(Func<Random, TInput, IEnumerable<TInput>> func) : IInputMutator<TInput>
    {
        public IEnumerable<TInput> Mutate(Random random, TInput input) => func(random, input);
    }
}
