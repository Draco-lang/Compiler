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
    /// <param name="input">The input to minimize.</param>
    /// <returns>A sequence of minimized inputs, that hopefully results in the same coverage.</returns>
    public IEnumerable<TInput> Minimize(TInput input);
}
