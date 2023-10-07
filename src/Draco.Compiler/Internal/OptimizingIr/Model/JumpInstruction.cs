using System.Collections.Generic;
using System.Linq;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// An unconditional jump.
/// </summary>
internal sealed class JumpInstruction : InstructionBase
{
    public override bool IsBranch => true;
    public override IEnumerable<BasicBlock> JumpTargets => new[] { this.Target };

    /// <summary>
    /// The jump target.
    /// </summary>
    public BasicBlock Target { get; set; }

    public JumpInstruction(BasicBlock target)
    {
        this.Target = target;
    }

    public override string ToString() => $"jump {this.Target.ToOperandString()}";

    public override JumpInstruction Clone() => new(this.Target);
}
