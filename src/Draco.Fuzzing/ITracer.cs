using System.Collections.Generic;
using Draco.Coverage;

namespace Draco.Fuzzing;

/// <summary>
/// Traces the whole fuzzing process.
/// </summary>
/// <typeparam name="TInput">The type of the input data.</typeparam>
public interface ITracer<TInput>
{
    /// <summary>
    /// Called when inputs were enqueued into the fuzzer.
    /// </summary>
    /// <param name="inputs">The inputs that were enqueued.</param>
    /// <param name="inputQueue">The current input queue.</param>
    public void InputsEnqueued(IEnumerable<TInput> inputs, IReadOnlyCollection<TInput> inputQueue);

    /// <summary>
    /// Called when an input was dequeued from the fuzzer.
    /// </summary>
    /// <param name="input">The input that was dequeued.</param>
    /// <param name="inputQueue">The current input queue.</param>
    public void InputDequeued(TInput input, IReadOnlyCollection<TInput> inputQueue);

    /// <summary>
    /// Called when the minimization of an input finishes.
    /// </summary>
    /// <param name="input">The original input.</param>
    /// <param name="minimizedInput">The minimized input.</param>
    /// <param name="coverage">The coverage of the minimized input.</param>
    public void EndOfMinimization(TInput input, TInput minimizedInput, CoverageResult coverage);

    /// <summary>
    /// Called when mutation of an input finishes.
    /// </summary>
    /// <param name="input">The original input.</param>
    /// <param name="mutationsFound">The number of mutations found.</param>
    public void EndOfMutations(TInput input, int mutationsFound);

    /// <summary>
    /// Called when an input faulted.
    /// </summary>
    /// <param name="input">The input that faulted.</param>
    /// <param name="fault">The fault result.</param>
    public void InputFaulted(TInput input, FaultResult fault);

    /// <summary>
    /// Called when the fuzzer finishes, because the queue is empty.
    /// </summary>
    public void FuzzerFinished();
}

/// <summary>
/// A tracer that does nothing.
/// </summary>
/// <typeparam name="T">The type of the input data.</typeparam>
public sealed class NullTracer<T> : ITracer<T>
{
    /// <summary>
    /// The singleton instance of the null tracer.
    /// </summary>
    public static NullTracer<T> Instance { get; } = new();

    private NullTracer()
    {
    }

    public void EndOfMinimization(T input, T minimizedInput, CoverageResult coverage) { }
    public void EndOfMutations(T input, int mutationsFound) { }
    public void FuzzerFinished() { }
    public void InputDequeued(T input, IReadOnlyCollection<T> inputQueue) { }
    public void InputFaulted(T input, FaultResult fault) { }
    public void InputsEnqueued(IEnumerable<T> inputs, IReadOnlyCollection<T> inputQueue) { }
}
