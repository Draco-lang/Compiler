using Draco.Compiler.Fuzzer.Generators;

namespace Draco.Compiler.Fuzzer.Components;

/// <summary>
/// Utility base-class for component fuzzers.
/// </summary>
/// <typeparam name="TInput">The input type.</typeparam>
internal abstract class ComponentFuzzerBase<TInput> : IComponentFuzzer
{
    private readonly IGenerator<TInput> inputGenerator;
    private TInput oldInput = default!;

    public ComponentFuzzerBase(IGenerator<TInput> inputGenerator)
    {
        this.inputGenerator = inputGenerator;
    }

    public void NextEpoch()
    {
        this.oldInput = this.inputGenerator.NextEpoch();
        try
        {
            this.NextEpochInternal(this.oldInput);
        }
        catch (Exception ex)
        {
            var inputString = this.inputGenerator.ToString(this.oldInput);
            throw new CrashException(inputString, ex);
        }
    }

    public void NextMutation()
    {
        var nextInput = this.inputGenerator.NextMutation();
        try
        {
            this.NextMutationInternal(this.oldInput, nextInput);
        }
        catch (Exception ex)
        {
            var oldInputString = this.inputGenerator.ToString(this.oldInput);
            var nextInputString = this.inputGenerator.ToString(nextInput);
            throw new MutationException(oldInputString, nextInputString, ex);
        }
    }

    protected abstract void NextEpochInternal(TInput input);
    protected abstract void NextMutationInternal(TInput oldInput, TInput newInput);
}
