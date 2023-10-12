using System.Collections.Generic;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// An array length query.
/// </summary>
internal sealed class ArrayLengthInstruction : InstructionBase, IValueInstruction
{
    public string InstructionKeyword => "length";

    public Register Target { get; set; }

    /// <summary>
    /// The array to get the length of.
    /// </summary>
    public IOperand Array { get; set; }

    public override IEnumerable<IOperand> Operands => new[] { this.Array };

    public ArrayLengthInstruction(Register target, IOperand array)
    {
        this.Target = target;
        this.Array = array;
    }

    public override string ToString() =>
        $"{this.Target.ToOperandString()} := {this.InstructionKeyword} {this.Array.ToOperandString()}";

    public override ArrayLengthInstruction Clone() => new(this.Target, this.Array);
}
