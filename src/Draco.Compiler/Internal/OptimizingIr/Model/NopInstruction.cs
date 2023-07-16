namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Represents no operation.
/// </summary>
internal sealed class NopInstruction : InstructionBase
{
    public override string ToString() => "nop";

    public override NopInstruction Clone() => new();
}
