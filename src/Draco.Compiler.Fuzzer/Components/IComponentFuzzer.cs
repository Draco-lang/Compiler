namespace Draco.Compiler.Fuzzer.Components;

/// <summary>
/// Represents the fuzzer of a compiler component.
/// </summary>
internal interface IComponentFuzzer
{
    /// <summary>
    /// Starts a new epoch for the component.
    /// </summary>
    public void NextEpoch();

    /// <summary>
    /// Mutates the input and feeds it into the component.
    /// </summary>
    public void NextMutation();
}
