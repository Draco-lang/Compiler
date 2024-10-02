using System.Collections.Generic;
using Draco.Coverage;

namespace Draco.Fuzzing.Tracing;

/// <summary>
/// A tracer that locks access to another tracer using a synchronization object.
/// </summary>
/// <typeparam name="TInput">The type of the input data.</typeparam>
/// <param name="inner">The inner tracer to lock access to.</param>
/// <param name="sync">The synchronization object to lock access with.</param>
public sealed class LockSyncTracer<TInput>(ITracer<TInput> inner, object sync) : ITracer<TInput>
{
    public LockSyncTracer(ITracer<TInput> inner)
        : this(inner, new())
    {
    }

    public void InputsEnqueued(IEnumerable<InputWithId<TInput>> inputs)
    {
        lock (sync) inner.InputsEnqueued(inputs);
    }

    public void InputDequeued(InputWithId<TInput> input)
    {
        lock (sync) inner.InputDequeued(input);
    }

    public void InputFuzzStarted(InputWithId<TInput> input, TargetInfo targetInfo)
    {
        lock (sync) inner.InputFuzzStarted(input, targetInfo);
    }

    public void InputFuzzEnded(InputWithId<TInput> input, TargetInfo targetInfo, CoverageResult coverageResult)
    {
        lock (sync) inner.InputFuzzEnded(input, targetInfo, coverageResult);
    }

    public void MinimizationFound(InputWithId<TInput> input, InputWithId<TInput> minimizedInput)
    {
        lock (sync) inner.MinimizationFound(input, minimizedInput);
    }

    public void MutationFound(InputWithId<TInput> input, InputWithId<TInput> mutatedInput)
    {
        lock (sync) inner.MutationFound(input, mutatedInput);
    }

    public void InputFaulted(InputWithId<TInput> input, FaultResult fault)
    {
        lock (sync) inner.InputFaulted(input, fault);
    }

    public void FuzzerStarted()
    {
        lock (sync) inner.FuzzerStarted();
    }

    public void FuzzerStopped()
    {
        lock (sync) inner.FuzzerStopped();
    }
}
