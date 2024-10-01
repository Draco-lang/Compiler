using System;
using System.Collections.Generic;
using Draco.Coverage;

namespace Draco.Fuzzing.Tracing;

public readonly record struct InputsEnqueuedEventArgs<TInput>(IEnumerable<TInput> Inputs);
public readonly record struct InputDequeuedEventArgs<TInput>(TInput Input);
public readonly record struct InputFuzzStartedEventArgs<TInput>(TInput Input, TargetInfo TargetInfo);
public readonly record struct InputFuzzEndedEventArgs<TInput>(TInput Input, TargetInfo TargetInfo, CoverageResult CoverageResult);
public readonly record struct MinimizationFoundEventArgs<TInput>(TInput Input, TInput MinimizedInput);
public readonly record struct MutationFoundEventArgs<TInput>(TInput Input, TInput MutatedInput);
public readonly record struct InputFaultedEventArgs<TInput>(TInput Input, FaultResult Fault);

/// <summary>
/// A tracer that raises events for each tracing event.
/// </summary>
/// <typeparam name="TInput">The type of the input data.</typeparam>
public sealed class EventTracer<TInput> : ITracer<TInput>
{
    public event EventHandler<InputsEnqueuedEventArgs<TInput>>? OnInputsEnqueued;
    public event EventHandler<InputDequeuedEventArgs<TInput>>? OnInputDequeued;
    public event EventHandler<InputFuzzStartedEventArgs<TInput>>? OnInputFuzzStarted;
    public event EventHandler<InputFuzzEndedEventArgs<TInput>>? OnInputFuzzEnded;
    public event EventHandler<MinimizationFoundEventArgs<TInput>>? OnMinimizationFound;
    public event EventHandler<MutationFoundEventArgs<TInput>>? OnMutationFound;
    public event EventHandler<InputFaultedEventArgs<TInput>>? OnInputFaulted;
    public event EventHandler? OnFuzzerStarted;
    public event EventHandler? OnFuzzerFinished;

    public void InputsEnqueued(IEnumerable<TInput> inputs) =>
        this.OnInputsEnqueued?.Invoke(this, new InputsEnqueuedEventArgs<TInput>(inputs));
    public void InputDequeued(TInput input) =>
        this.OnInputDequeued?.Invoke(this, new InputDequeuedEventArgs<TInput>(input));
    public void InputFuzzStarted(TInput input, TargetInfo targetInfo) =>
        this.OnInputFuzzStarted?.Invoke(this, new InputFuzzStartedEventArgs<TInput>(input, targetInfo));
    public void InputFuzzEnded(TInput input, TargetInfo targetInfo, CoverageResult coverageResult) =>
        this.OnInputFuzzEnded?.Invoke(this, new InputFuzzEndedEventArgs<TInput>(input, targetInfo, coverageResult));
    public void MinimizationFound(TInput input, TInput minimizedInput) =>
        this.OnMinimizationFound?.Invoke(this, new MinimizationFoundEventArgs<TInput>(input, minimizedInput));
    public void MutationFound(TInput input, TInput mutatedInput) =>
        this.OnMutationFound?.Invoke(this, new MutationFoundEventArgs<TInput>(input, mutatedInput));
    public void InputFaulted(TInput input, FaultResult fault) =>
        this.OnInputFaulted?.Invoke(this, new InputFaultedEventArgs<TInput>(input, fault));
    public void FuzzerStarted() => this.OnFuzzerStarted?.Invoke(this, EventArgs.Empty);
    public void FuzzerFinished() => this.OnFuzzerFinished?.Invoke(this, EventArgs.Empty);
}
