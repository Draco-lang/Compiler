using System.Collections.Generic;
using System.Linq;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A conditional jump.
/// </summary>
internal sealed class BranchInstruction : InstructionBase
{
    public override bool IsBranch => true;
    public override IEnumerable<BasicBlock> JumpTargets => new[] { this.Then, this.Else };

    /// <summary>
    /// The condition to base the jump on.
    /// </summary>
    public IOperand Condition { get; set; }

    /// <summary>
    /// The target to jump to in case a truthy condition.
    /// </summary>
    public BasicBlock Then { get; set; }

    /// <summary>
    /// The target to jump to in case a falsy condition.
    /// </summary>
    public BasicBlock Else { get; set; }

    public BranchInstruction(IOperand condition, BasicBlock then, BasicBlock @else)
    {
        this.Condition = condition;
        this.Then = then;
        this.Else = @else;
    }

    public override string ToString() =>
        $"if {this.Condition.ToOperandString()} jump lbl{this.Then.Index} else jump lbl{this.Else.Index}";

    public override BranchInstruction Clone() => new(this.Condition, this.Then, this.Else);
}
