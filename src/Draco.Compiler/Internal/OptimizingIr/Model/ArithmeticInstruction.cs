using System;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Represents some kind of binary arithmetic instruction.
/// </summary>
internal sealed class ArithmeticInstruction : InstructionBase
{
    /// <summary>
    /// The register to store the result at.
    /// </summary>
    public Register Target { get; set; }

    /// <summary>
    /// The arithmetic operation performed.
    /// </summary>
    public ArithmeticOp Op { get; set; }

    /// <summary>
    /// The left operand of the operation.
    /// </summary>
    public IOperand Left { get; set; }

    /// <summary>
    /// The right operand of the operation.
    /// </summary>
    public IOperand Right { get; set; }

    public ArithmeticInstruction(Register target, ArithmeticOp op, IOperand left, IOperand right)
    {
        this.Target = target;
        this.Op = op;
        this.Left = left;
        this.Right = right;
    }

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
