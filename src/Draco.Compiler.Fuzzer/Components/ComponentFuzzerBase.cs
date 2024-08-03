using Draco.Compiler.Fuzzer.Generators;

namespace Draco.Compiler.Fuzzer.Components;

/// <summary>
/// Utility base-class for component fuzzers.
/// </summary>
/// <typeparam name="TInput">The input type.</typeparam>
internal abstract class ComponentFuzzerBase<TInput>(IGenerator<TInput> inputGenerator) : IComponentFuzzer
{
    private TInput oldInput = default!;

    public void NextEpoch()
    {
        this.oldInput = inputGenerator.NextEpoch();
        try
        {
            this.NextEpochInternal(this.oldInput);
        }
        catch (Exception ex)
        {
            var inputString = inputGenerator.ToString(this.oldInput);
            throw new CrashException(inputString, ex);
        }
    }

    public void NextMutation()
    {
        var nextInput = inputGenerator.NextMutation();
        try
        {
            this.NextMutationInternal(this.oldInput, nextInput);
        }
        catch (Exception ex)
        {
            var oldInputString = inputGenerator.ToString(this.oldInput);
            var nextInputString = inputGenerator.ToString(nextInput);
            throw new MutationException(oldInputString, nextInputString, ex);
        }
    }

    protected abstract void NextEpochInternal(TInput input);
    protected abstract void NextMutationInternal(TInput oldInput, TInput newInput);
}
