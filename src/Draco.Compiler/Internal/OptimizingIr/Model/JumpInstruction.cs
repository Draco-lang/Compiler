using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// An unconditional jump.
/// </summary>
internal sealed class JumpInstruction : InstructionBase
{
    public override bool IsBranch => true;
    public override IEnumerable<BasicBlock> JumpTargets => new[] { this.Target };
    public override IEnumerable<IOperand> Operands => new[] { this.Target };

    /// <summary>
    /// The jump target.
    /// </summary>
    public BasicBlock Target { get; set; }

    public JumpInstruction(BasicBlock target)
    {
        this.Target = target;
    }
}
