using System.Collections.Generic;
using Draco.Coverage;

namespace Draco.Fuzzing.Tracing;

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
