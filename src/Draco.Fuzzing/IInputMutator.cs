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
    /// <param name="input">The input to mutate.</param>
    /// <returns>A sequence of mutated inputs.</returns>
    public IEnumerable<TInput> Mutate(TInput input);
}
