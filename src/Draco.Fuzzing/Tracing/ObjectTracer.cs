using System.Collections.Generic;
using System.Linq;
using Draco.Coverage;

namespace Draco.Fuzzing.Tracing;

/// <summary>
/// A tracer that type-erases the input type by casting it to a <see cref="System.Object"/>.
/// </summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <param name="inner">The inner, type-erased tracer.</param>
public sealed class ObjectTracer<TInput>(ITracer<object?> inner) : ITracer<TInput>
{
    public void InputsEnqueued(IEnumerable<InputWithId<TInput>> inputs) =>
        inner.InputsEnqueued(inputs.Select(Erase));
    public void InputDequeued(InputWithId<TInput> input) =>
        inner.InputDequeued(Erase(input));
    public void InputFuzzStarted(InputWithId<TInput> input, TargetInfo targetInfo) =>
        inner.InputFuzzStarted(Erase(input), targetInfo);
    public void InputFuzzEnded(InputWithId<TInput> input, TargetInfo targetInfo, CoverageResult coverageResult) =>
        inner.InputFuzzEnded(Erase(input), targetInfo, coverageResult);
    public void MinimizationFound(InputWithId<TInput> input, InputWithId<TInput> minimizedInput) =>
        inner.MinimizationFound(Erase(input), Erase(minimizedInput));
    public void MutationFound(InputWithId<TInput> input, InputWithId<TInput> mutatedInput) =>
        inner.MutationFound(Erase(input), Erase(mutatedInput));
    public void InputFaulted(InputWithId<TInput> input, FaultResult fault) =>
        inner.InputFaulted(Erase(input), fault);
    public void FuzzerStarted() => inner.FuzzerStarted();
    public void FuzzerFinished() => inner.FuzzerFinished();

    private static InputWithId<object?> Erase(InputWithId<TInput> input) => new(input.Id, input.Input);
}
