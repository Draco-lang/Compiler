using System.Collections.Generic;
using System.Linq;
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
    public void InputsEnqueued(IEnumerable<TInput> inputs);

    /// <summary>
    /// Called when an input was dequeued from the fuzzer.
    /// </summary>
    /// <param name="input">The input that was dequeued.</param>
    public void InputDequeued(TInput input);

    /// <summary>
    /// The fuzzing of some input started.
    /// </summary>
    /// <param name="targetInfo">The target information.</param>
    /// <param name="input">The input to be fuzzed.</param>
    public void InputFuzzStarted(TInput input, TargetInfo targetInfo);

    /// <summary>
    /// The fuzzing of some input completed.
    /// </summary>
    /// <param name="input">The input that was fuzzed.</param>
    /// <param name="targetInfo">The target information.</param>
    /// <param name="coverageResult">The coverage of the input.</param>
    public void InputFuzzEnded(TInput input, TargetInfo targetInfo, CoverageResult coverageResult);

    /// <summary>
    /// Called when a smaller input was found.
    /// </summary>
    /// <param name="input">The original input.</param>
    /// <param name="minimizedInput">The minimized input.</param>
    public void MinimizationFound(TInput input, TInput minimizedInput);

    /// <summary>
    /// Called when a mutation is found.
    /// </summary>
    /// <param name="input">The original input.</param>
    /// <param name="mutatedInput">The mutated input.</param>
    public void MutationFound(TInput input, TInput mutatedInput);

    /// <summary>
    /// Called when an input faulted.
    /// </summary>
    /// <param name="input">The input that faulted.</param>
    /// <param name="fault">The fault result.</param>
    public void InputFaulted(TInput input, FaultResult fault);

    /// <summary>
    /// Called when the fuzzer starts.
    /// </summary>
    public void FuzzerStarted();

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

    public void InputsEnqueued(IEnumerable<T> inputs) { }
    public void InputDequeued(T input) { }
    public void InputFuzzStarted(T input, TargetInfo targetInfo) { }
    public void InputFuzzEnded(T input, TargetInfo targetInfo, CoverageResult coverageResult) { }
    public void MinimizationFound(T input, T minimizedInput) { }
    public void MutationFound(T input, T mutatedInput) { }
    public void InputFaulted(T input, FaultResult fault) { }
    public void FuzzerStarted() { }
    public void FuzzerFinished() { }
}

/// <summary>
/// A tracer that broadcasts to multiple tracers.
/// </summary>
/// <typeparam name="T">The type of the input data.</typeparam>
/// <param name="tracers">The tracers to broadcast to.</param>
public sealed class BroadcastTracer<T>(IEnumerable<ITracer<T>> tracers) : ITracer<T>
{
    private readonly List<ITracer<T>> tracers = tracers.ToList();

    public void InputsEnqueued(IEnumerable<T> inputs)
    {
        foreach (var tracer in this.tracers) tracer.InputsEnqueued(inputs);
    }

    public void InputDequeued(T input)
    {
        foreach (var tracer in this.tracers) tracer.InputDequeued(input);
    }

    public void InputFuzzStarted(T input, TargetInfo targetInfo)
    {
        foreach (var tracer in this.tracers) tracer.InputFuzzStarted(input, targetInfo);
    }

    public void InputFuzzEnded(T input, TargetInfo targetInfo, CoverageResult coverageResult)
    {
        foreach (var tracer in this.tracers) tracer.InputFuzzEnded(input, targetInfo, coverageResult);
    }

    public void MinimizationFound(T input, T minimizedInput)
    {
        foreach (var tracer in this.tracers) tracer.MinimizationFound(input, minimizedInput);
    }

    public void MutationFound(T input, T mutatedInput)
    {
        foreach (var tracer in this.tracers) tracer.MutationFound(input, mutatedInput);
    }

    public void InputFaulted(T input, FaultResult fault)
    {
        foreach (var tracer in this.tracers) tracer.InputFaulted(input, fault);
    }

    public void FuzzerStarted()
    {
        foreach (var tracer in this.tracers) tracer.FuzzerStarted();
    }

    public void FuzzerFinished()
    {
        foreach (var tracer in this.tracers) tracer.FuzzerFinished();
    }
}
