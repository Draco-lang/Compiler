using System.Collections.Generic;
using System.Linq;
using Draco.Coverage;

namespace Draco.Fuzzing.Tracing;

/// <summary>
/// A tracer that broadcasts to multiple tracers.
/// </summary>
/// <typeparam name="TInput">The type of the input data.</typeparam>
/// <param name="tracers">The tracers to broadcast to.</param>
public sealed class BroadcastTracer<TInput>(IEnumerable<ITracer<TInput>> tracers) : ITracer<TInput>
{
    private readonly List<ITracer<TInput>> tracers = tracers.ToList();

    public void InputsEnqueued(IEnumerable<InputWithId<TInput>> inputs)
    {
        foreach (var tracer in this.tracers) tracer.InputsEnqueued(inputs);
    }

    public void InputDequeued(InputWithId<TInput> input)
    {
        foreach (var tracer in this.tracers) tracer.InputDequeued(input);
    }

    public void InputDropped(InputWithId<TInput> input)
    {
        foreach (var tracer in this.tracers) tracer.InputDropped(input);
    }

    public void InputFuzzStarted(InputWithId<TInput> input, TargetInfo targetInfo)
    {
        foreach (var tracer in this.tracers) tracer.InputFuzzStarted(input, targetInfo);
    }

    public void InputFuzzEnded(InputWithId<TInput> input, TargetInfo targetInfo, CoverageResult coverageResult)
    {
        foreach (var tracer in this.tracers) tracer.InputFuzzEnded(input, targetInfo, coverageResult);
    }

    public void MinimizationFound(InputWithId<TInput> input, InputWithId<TInput> minimizedInput)
    {
        foreach (var tracer in this.tracers) tracer.MinimizationFound(input, minimizedInput);
    }

    public void MutationFound(InputWithId<TInput> input, InputWithId<TInput> mutatedInput)
    {
        foreach (var tracer in this.tracers) tracer.MutationFound(input, mutatedInput);
    }

    public void InputFaulted(InputWithId<TInput> input, FaultResult fault)
    {
        foreach (var tracer in this.tracers) tracer.InputFaulted(input, fault);
    }

    public void FuzzerStarted()
    {
        foreach (var tracer in this.tracers) tracer.FuzzerStarted();
    }

    public void FuzzerStopped()
    {
        foreach (var tracer in this.tracers) tracer.FuzzerStopped();
    }
}
