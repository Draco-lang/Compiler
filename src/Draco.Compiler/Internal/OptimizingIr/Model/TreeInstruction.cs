using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Represents a tree-ified instruction, that's only part of the stackification process.
/// Not an actual instruction, it's a wrapper to signal structural changes.
/// </summary>
internal sealed class TreeInstruction : InstructionBase, IOperand, IValueInstruction
{
    public string InstructionKeyword => this.Underlying.InstructionKeyword;

    public Register Target => this.Underlying.Target;

    /// <summary>
    /// The original, non-tree instruction.
    /// </summary>
    public IValueInstruction Underlying { get; }

    public override bool IsBranch => this.Underlying.IsBranch;
    public override IEnumerable<BasicBlock> JumpTargets => this.Underlying.JumpTargets.Cast<BasicBlock>();
    public override IEnumerable<IOperand> Operands { get; }

    public TypeSymbol Type => this.Target.Type;

    public TreeInstruction(IValueInstruction underlying, ImmutableArray<IOperand> operands)
    {
        this.Underlying = underlying;
        this.Operands = operands;
    }

    public override string ToString() => $"{this.Target} := {this.ToOperandString()}";
    public string ToOperandString() => $"{this.InstructionKeyword}({string.Join(", ", this.Operands.Select(op => op.ToOperandString()))})";
    public override IInstruction Clone() =>
        new TreeInstruction((IValueInstruction)this.Underlying.Clone(), this.Operands.ToImmutableArray());
}
