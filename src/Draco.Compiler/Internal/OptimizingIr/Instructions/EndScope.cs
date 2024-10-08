namespace Draco.Compiler.Internal.OptimizingIr.Instructions;

/// <summary>
/// A pseudo-instruction for representing the end of a local scope.
/// </summary>
internal sealed class EndScope : InstructionBase
{
    public override string InstructionKeyword => "@scope end";

    public override bool IsValidInUnreachableContext => true;

    public override EndScope Clone() => new();

    public override string ToString() => this.InstructionKeyword;
}
