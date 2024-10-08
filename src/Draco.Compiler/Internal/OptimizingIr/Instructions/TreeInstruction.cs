using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Instructions;

/// <summary>
/// Represents a tree-ified instruction, that's only part of the stackification process.
/// Not an actual instruction, it's a wrapper to signal structural changes.
/// </summary>
internal sealed class TreeInstruction : InstructionBase, IOperand, IValueInstruction
{
    public override string InstructionKeyword => this.Underlying.InstructionKeyword;

    public Register Target => ((IValueInstruction)this.Underlying).Target;

    /// <summary>
    /// The original, non-tree instruction.
    /// </summary>
    public IInstruction Underlying { get; }

    public override bool IsBranch => this.Underlying.IsBranch;
    public override IEnumerable<BasicBlock> JumpTargets => this.Underlying.JumpTargets.Cast<BasicBlock>();
    public override IEnumerable<Symbol> StaticOperands => this.Underlying.StaticOperands;
    public override IEnumerable<IOperand> Operands { get; }

    public TypeSymbol Type => this.Target.Type;

    public TreeInstruction(IInstruction underlying, ImmutableArray<IOperand> operands)
    {
        if (underlying is TreeInstruction) throw new ArgumentOutOfRangeException(nameof(underlying));

        this.Underlying = underlying;
        this.Operands = operands;
    }

    public override string ToString() => this.Underlying is IValueInstruction
        ? $"{this.Target} := {this.ToOperandString()}"
        : this.ToOperandString();
    public string ToOperandString()
    {
        var staticOperands = this.StaticOperands.Select(op => op.FullName);
        var operands = this.Operands.Select(op => op.ToOperandString());
        var jumpTargets = this.JumpTargets.Select(op => $"lbl{op.Index}");
        var allArgs = staticOperands.Concat(operands).Concat(jumpTargets);
        return $"{this.InstructionKeyword}({string.Join(", ", allArgs)})";
    }
    public override IInstruction Clone() =>
        new TreeInstruction(this.Underlying.Clone(), this.Operands.ToImmutableArray());
}
