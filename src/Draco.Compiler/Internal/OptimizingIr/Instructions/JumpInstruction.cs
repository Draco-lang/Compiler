using System.Collections.Generic;
using Draco.Compiler.Internal.OptimizingIr.Model;

namespace Draco.Compiler.Internal.OptimizingIr.Instructions;

/// <summary>
/// An unconditional jump.
/// </summary>
internal sealed class JumpInstruction(BasicBlock target) : InstructionBase
{
    public override string InstructionKeyword => "jump";
    public override bool IsBranch => true;
    public override IEnumerable<BasicBlock> JumpTargets => [this.Target];

    /// <summary>
    /// The jump target.
    /// </summary>
    public BasicBlock Target { get; set; } = target;

    public override string ToString() => $"{this.InstructionKeyword} lbl{this.Target.Index}";

    public override JumpInstruction Clone() => new(this.Target);
}
