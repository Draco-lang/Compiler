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

    /// <summary>
    /// Creates a mutator that randomly swaps two elements in a sequence.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in the sequence.</typeparam>
    /// <returns>The mutator.</returns>
    public static IInputMutator<IList<TElement>> Swap<TElement>() => Create<IList<TElement>>(SwapDelegate);

    private static IEnumerable<IList<TElement>> SwapDelegate<TElement>(Random random, IList<TElement> input)
    {
        // Can't mutate a sequence with less than 2 elements
        if (input.Count < 2) yield return input;

        while (true)
        {
            var index1 = random.Next(input.Count);
            var index2 = random.Next(input.Count);

            if (index1 == index2) continue;

            (input[index1], input[index2]) = (input[index2], input[index1]);
            yield return input;
        }
    }

    private sealed class DelegateMutator<TInput>(Func<Random, TInput, IEnumerable<TInput>> func) : IInputMutator<TInput>
    {
        public IEnumerable<TInput> Mutate(Random random, TInput input) => func(random, input);
    }
}
