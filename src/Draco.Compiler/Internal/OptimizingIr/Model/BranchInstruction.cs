using System.Collections.Generic;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A conditional jump.
/// </summary>
internal sealed class BranchInstruction(IOperand condition, BasicBlock then, BasicBlock @else)
    : InstructionBase
{
    public override string InstructionKeyword => "jump_if";
    public override bool IsBranch => true;
    public override IEnumerable<IOperand> Operands => [this.Condition];
    public override IEnumerable<BasicBlock> JumpTargets => [this.Then, this.Else];

    /// <summary>
    /// The condition to base the jump on.
    /// </summary>
    public IOperand Condition { get; set; } = condition;

    /// <summary>
    /// The target to jump to in case a truthy condition.
    /// </summary>
    public BasicBlock Then { get; set; } = then;

    /// <summary>
    /// The target to jump to in case a falsy condition.
    /// </summary>
    public BasicBlock Else { get; set; } = @else;

    public override string ToString() =>
        $"if {this.Condition.ToOperandString()} jump lbl{this.Then.Index} else jump lbl{this.Else.Index}";

    public override BranchInstruction Clone() => new(this.Condition, this.Then, this.Else);
}
