using System.Collections.Generic;
using Draco.Coverage;

namespace Draco.Fuzzing.Tracing;

/// <summary>
/// A tracer that does nothing.
/// </summary>
/// <typeparam name="TInput">The type of the input data.</typeparam>
public sealed class NullTracer<TInput> : ITracer<TInput>
{
    /// <summary>
    /// The singleton instance of the null tracer.
    /// </summary>
    public static NullTracer<TInput> Instance { get; } = new();

    private NullTracer()
    {
    }

    public void InputsEnqueued(IEnumerable<InputWithId<TInput>> inputs) { }
    public void InputDequeued(InputWithId<TInput> input) { }
    public void InputFuzzStarted(InputWithId<TInput> input, TargetInfo targetInfo) { }
    public void InputFuzzEnded(InputWithId<TInput> input, TargetInfo targetInfo, CoverageResult coverageResult) { }
    public void MinimizationFound(InputWithId<TInput> input, InputWithId<TInput> minimizedInput) { }
    public void MutationFound(InputWithId<TInput> input, InputWithId<TInput> mutatedInput) { }
    public void InputFaulted(InputWithId<TInput> input, FaultResult fault) { }
    public void FuzzerStarted() { }
    public void FuzzerFinished() { }
}
