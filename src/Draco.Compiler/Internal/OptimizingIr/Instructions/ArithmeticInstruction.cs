using System;
using System.Collections.Generic;
using Draco.Compiler.Internal.OptimizingIr.Model;

namespace Draco.Compiler.Internal.OptimizingIr.Instructions;

/// <summary>
/// Represents some kind of binary arithmetic instruction.
/// </summary>
internal sealed class ArithmeticInstruction(
    Register target,
    ArithmeticOp op,
    IOperand left,
    IOperand right) : InstructionBase, IValueInstruction
{
    public override string InstructionKeyword => this.OpToString();

    public Register Target { get; set; } = target;

    /// <summary>
    /// The arithmetic operation performed.
    /// </summary>
    public ArithmeticOp Op { get; set; } = op;

    /// <summary>
    /// The left operand of the operation.
    /// </summary>
    public IOperand Left { get; set; } = left;

    /// <summary>
    /// The right operand of the operation.
    /// </summary>
    public IOperand Right { get; set; } = right;

    public override IEnumerable<IOperand> Operands => [this.Left, this.Right];

    public override string ToString() =>
        $"{this.Target.ToOperandString()} := {this.Left.ToOperandString()} {this.OpToString()} {this.Right.ToOperandString()}";

    private string OpToString() => this.Op switch
    {
        ArithmeticOp.Add => "+",
        ArithmeticOp.Sub => "-",
        ArithmeticOp.Mul => "*",
        ArithmeticOp.Div => "/",
        ArithmeticOp.Rem => "rem",
        ArithmeticOp.Less => "<",
        ArithmeticOp.Equal => "=",
        _ => throw new InvalidOperationException(),
    };

    public override ArithmeticInstruction Clone() => new(this.Target, this.Op, this.Left, this.Right);
}
