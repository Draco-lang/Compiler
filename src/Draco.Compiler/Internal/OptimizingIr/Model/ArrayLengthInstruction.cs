using System.Collections.Generic;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// An array length query.
/// </summary>
internal sealed class ArrayLengthInstruction(Register target, IOperand array)
    : InstructionBase, IValueInstruction
{
    public override string InstructionKeyword => "length";

    public Register Target { get; set; } = target;

    /// <summary>
    /// The array to get the length of.
    /// </summary>
    public IOperand Array { get; set; } = array;

    public override IEnumerable<IOperand> Operands => [this.Array];

    public override string ToString() =>
        $"{this.Target.ToOperandString()} := {this.InstructionKeyword} {this.Array.ToOperandString()}";

    public override ArrayLengthInstruction Clone() => new(this.Target, this.Array);
}
