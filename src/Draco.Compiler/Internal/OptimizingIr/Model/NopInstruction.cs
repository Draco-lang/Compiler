namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Represents no operation.
/// </summary>
internal sealed class NopInstruction : InstructionBase
{
    public override NopInstruction Clone() => new();
}
