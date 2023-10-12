using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A mutable base class for <see cref="IInstruction"/> implementations.
/// </summary>
internal abstract class InstructionBase : IInstruction
{
    public abstract string InstructionKeyword { get; }

    public BasicBlock BasicBlock { get; set; } = null!;
    IBasicBlock IInstruction.BasicBlock => this.BasicBlock;
    public InstructionBase? Prev { get; set; }
    IInstruction? IInstruction.Prev => this.Prev;
    public InstructionBase? Next { get; set; }
    IInstruction? IInstruction.Next => this.Next;
    public virtual bool IsBranch => false;
    public virtual bool IsValidInUnreachableContext => false;
    public virtual IEnumerable<BasicBlock> JumpTargets => Enumerable.Empty<BasicBlock>();
    IEnumerable<IBasicBlock> IInstruction.JumpTargets => this.JumpTargets;
    public virtual IEnumerable<Symbol> StaticOperands => Enumerable.Empty<Symbol>();
    public virtual IEnumerable<IOperand> Operands => Enumerable.Empty<IOperand>();

    public override abstract string ToString();

    public abstract IInstruction Clone();
}
