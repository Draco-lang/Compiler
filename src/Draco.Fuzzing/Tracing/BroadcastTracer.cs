using System.Collections.Generic;
using System.Linq;
using Draco.Coverage;

namespace Draco.Fuzzing.Tracing;

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
