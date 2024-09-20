using System;

namespace Draco.Fuzzing;

/// <summary>
/// Traces the whole fuzzing process.
/// </summary>
/// <typeparam name="TInput">The type of the input data.</typeparam>
public interface ITracer<TInput>
{
    /// <summary>
    /// Called when the minimization of an input finishes.
    /// </summary>
    /// <param name="input">The original input.</param>
    /// <param name="minimizedInput">The minimized input.</param>
    /// <param name="elapsed">The time it took to minimize the input.</param>
    public void EndOfMinimization(TInput input, TInput minimizedInput, TimeSpan elapsed);

    /// <summary>
    /// Called when mutation of an input finishes.
    /// </summary>
    /// <param name="input">The original input.</param>
    /// <param name="mutationsFound">The number of mutations found.</param>
    /// <param name="elapsed">The time it took to mutate the input.</param>
    public void EndOfMutations(TInput input, int mutationsFound, TimeSpan elapsed);

    /// <summary>
    /// Called when an input faulted.
    /// </summary>
    /// <param name="input">The input that faulted.</param>
    /// <param name="fault">The fault result.</param>
    public void InputFaulted(TInput input, FaultResult fault);
}
