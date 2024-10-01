using System.Collections.Generic;
using Draco.Coverage;

namespace Draco.Fuzzing.Tracing;

/// <summary>
/// An input with an identifier for tracing purposes.
/// </summary>
/// <typeparam name="TInput">The type of the input.</typeparam>
/// <param name="Id">The identifier of the input.</param>
/// <param name="Input">The input data.</param>
public readonly record struct InputWithId<TInput>(int Id, TInput Input);

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
    public void InputsEnqueued(IEnumerable<InputWithId<TInput>> inputs);

    /// <summary>
    /// Called when an input was dequeued from the fuzzer.
    /// </summary>
    /// <param name="input">The input that was dequeued.</param>
    public void InputDequeued(InputWithId<TInput> input);

    /// <summary>
    /// The fuzzing of some input started.
    /// </summary>
    /// <param name="targetInfo">The target information.</param>
    /// <param name="input">The input to be fuzzed.</param>
    public void InputFuzzStarted(InputWithId<TInput> input, TargetInfo targetInfo);

    /// <summary>
    /// The fuzzing of some input completed.
    /// </summary>
    /// <param name="input">The input that was fuzzed.</param>
    /// <param name="targetInfo">The target information.</param>
    /// <param name="coverageResult">The coverage of the input.</param>
    public void InputFuzzEnded(InputWithId<TInput> input, TargetInfo targetInfo, CoverageResult coverageResult);

    /// <summary>
    /// Called when a smaller input was found.
    /// </summary>
    /// <param name="input">The original input.</param>
    /// <param name="minimizedInput">The minimized input.</param>
    public void MinimizationFound(InputWithId<TInput> input, TInput minimizedInput);

    /// <summary>
    /// Called when a mutation is found.
    /// </summary>
    /// <param name="input">The original input.</param>
    /// <param name="mutatedInput">The mutated input.</param>
    public void MutationFound(InputWithId<TInput> input, TInput mutatedInput);

    /// <summary>
    /// Called when an input faulted.
    /// </summary>
    /// <param name="input">The input that faulted.</param>
    /// <param name="fault">The fault result.</param>
    public void InputFaulted(InputWithId<TInput> input, FaultResult fault);

    /// <summary>
    /// Called when the fuzzer starts.
    /// </summary>
    public void FuzzerStarted();

    /// <summary>
    /// Called when the fuzzer finishes, because the queue is empty.
    /// </summary>
    public void FuzzerFinished();
}
