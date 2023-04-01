using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A mutable implementation of <see cref="IBasicBlock"/>.
/// </summary>
internal sealed class BasicBlock : IBasicBlock
{
    public int Index { get; }
    public LabelSymbol Symbol { get; }
    public Procedure Procedure { get; }
    IProcedure IBasicBlock.Procedure => this.Procedure;

    public IInstruction FirstInstruction => this.firstInstruction
                                         ?? throw new InvalidOperationException("there is no first instruction");
    public IInstruction LastInstruction => this.lastInstruction
                                        ?? throw new InvalidOperationException("there is no last instruction");
    public IEnumerable<IInstruction> Instructions
    {
        get
        {
            for (var instr = this.FirstInstruction; instr is not null; instr = instr.Next) yield return instr;
        }
    }
    public int InstructionCount { get; private set; }
    public IEnumerable<BasicBlock> Successors => (this.lastInstruction?.IsBranch ?? false)
        ? this.lastInstruction.JumpTargets.Cast<BasicBlock>()
        : throw new InvalidOperationException("the last instruction of the block was not a jump");
    IEnumerable<IBasicBlock> IBasicBlock.Successors => this.Successors;

    private IInstruction? firstInstruction;
    private IInstruction? lastInstruction;

    public BasicBlock(Procedure procedure, LabelSymbol symbol, int index)
    {
        this.Procedure = procedure;
        this.Symbol = symbol;
        this.Index = index;
    }

    public override string ToString()
    {
        var result = new StringBuilder();
        result.Append(this.ToOperandString()).Append(':');
        if (!string.IsNullOrWhiteSpace(this.Symbol.Name)) result.Append($" ; {this.Symbol.Name}");
        result.AppendLine();
        result.AppendJoin(Environment.NewLine, this.Instructions.Select(i => $"  {i}"));
        return result.ToString();
    }
    public string ToOperandString() => $"lbl{this.Index}";

    private void AssertOwnInstruction(InstructionBase instruction)
    {
        if (!ReferenceEquals(this, instruction.BasicBlock))
        {
            throw new InvalidOperationException("instruction does not belong to basic block");
        }
    }

    private void AssignThisBasicBlock(InstructionBase instruction)
    {
        if (instruction.BasicBlock is not null)
        {
            throw new InvalidOperationException("instruction already belongs to a basic block");
        }
        instruction.BasicBlock = this;
    }

    public void InsertBefore(IInstruction existing, IInstruction added)
    {
        var existingBase = (InstructionBase)existing;
        var addedBase = (InstructionBase)added;

        this.AssertOwnInstruction(existingBase);
        this.AssignThisBasicBlock(addedBase);

        addedBase.Next = existingBase;
        if (existingBase.Prev is null)
        {
            addedBase.Prev = null;
            this.firstInstruction = addedBase;
        }
        else
        {
            addedBase.Prev = existingBase.Prev;
            existingBase.Prev.Next = addedBase;
        }
        existingBase.Prev = addedBase;
        ++this.InstructionCount;
    }

    public void InsertAfter(IInstruction existing, IInstruction added)
    {
        var existingBase = (InstructionBase)existing;
        var addedBase = (InstructionBase)added;

        this.AssertOwnInstruction(existingBase);
        this.AssignThisBasicBlock(addedBase);

        addedBase.Prev = existingBase;
        if (existingBase.Next is null)
        {
            addedBase.Next = null;
            this.lastInstruction = addedBase;
        }
        else
        {
            addedBase.Next = existingBase.Next;
            existingBase.Next.Prev = addedBase;
        }
        existingBase.Next = addedBase;
        ++this.InstructionCount;
    }

    public void InsertFirst(IInstruction added)
    {
        var addedBase = (InstructionBase)added;

        if (this.firstInstruction is null)
        {
            this.AssignThisBasicBlock(addedBase);
            this.firstInstruction = added;
            this.lastInstruction = added;
            addedBase.Prev = null;
            addedBase.Next = null;
            ++this.InstructionCount;
        }
        else
        {
            this.InsertBefore(this.firstInstruction, added);
        }
    }

    public void InsertLast(IInstruction added)
    {
        if (this.lastInstruction is null)
        {
            this.InsertFirst(added);
        }
        else
        {
            this.InsertAfter(this.lastInstruction, added);
        }
    }

    public void Remove(IInstruction removed)
    {
        var removedBase = (InstructionBase)removed;

        this.AssertOwnInstruction(removedBase);
        // Detach
        removedBase.BasicBlock = null!;

        if (removedBase.Prev is null) this.firstInstruction = removedBase.Next;
        else removedBase.Prev.Next = removedBase.Next;

        if (removedBase.Next is null) this.lastInstruction = removedBase.Prev;
        else removedBase.Next.Prev = removedBase.Prev;

        --this.InstructionCount;
    }
}
