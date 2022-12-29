using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.DracoIr;

/// <summary>
/// Represents a label during the instruction writing process.
/// </summary>
/// <param name="Target">The <see cref="BasicBlock"/> this <see cref="Label"/> marks.</param>
internal readonly record struct Label(BasicBlock Target);

/// <summary>
/// Helper to write <see cref="Instruction"/>s to a <see cref="DracoIr.Procedure"/>.
/// </summary>
internal sealed class InstructionWriter
{
    /// <summary>
    /// The <see cref="DracoIr.Procedure"/> being written.
    /// </summary>
    public Procedure Procedure { get; }

    /// <summary>
    /// The current <see cref="BasicBlock"/> being written.
    /// </summary>
    public BasicBlock CurrentBlock
    {
        get => this.currentBlock ?? throw new InvalidOperationException("there is no current basic block");
        set
        {
            this.currentBlock = value;
            this.instructionIndex = value.Instructions.Count;
        }
    }

    /// <summary>
    /// The index to write instructions to within the <see cref="CurrentBlock"/>.
    /// </summary>
    public int Index
    {
        get => this.instructionIndex;
        set
        {
            if (this.currentBlock is null) throw new InvalidOperationException("there is no current basic block");
            if (value < 0 || value > this.currentBlock.Instructions.Count) throw new ArgumentOutOfRangeException(nameof(value));
            this.instructionIndex = value;
        }
    }

    /// <summary>
    /// True, if the last instruction was some kind of branching.
    /// </summary>
    public bool EndsInBranch => this.currentBlock is null
                             || (this.currentBlock.Instructions.Count > this.instructionIndex
                              && this.currentBlock.Instructions[this.instructionIndex].IsBranch);

    private BasicBlock? currentBlock;
    private int instructionIndex;

    public InstructionWriter(Procedure procedure)
    {
        this.Procedure = procedure;
        this.CurrentBlock = procedure.BasicBlocks[^1];
    }

    /// <summary>
    /// Moves to the given <see cref="BasicBlock"/> and the given instruction index.
    /// </summary>
    /// <param name="block">The block to seek to.</param>
    /// <param name="index">The index within <paramref name="block"/> to seek to.</param>
    public void Seek(BasicBlock block, int index)
    {
        this.currentBlock = block;
        this.Index = index;
    }

    /// <summary>
    /// Seeks to the start of <paramref name="block"/>. See <see cref="Seek(BasicBlock, int)"/>.
    /// </summary>
    public void SeekStart(BasicBlock block) => this.Seek(block, 0);

    /// <summary>
    /// Seeks to the end of <paramref name="block"/>. See <see cref="Seek(BasicBlock, int)"/>.
    /// </summary>
    public void SeekEnd(BasicBlock block) => this.Seek(block, block.Instructions.Count);

    /// <summary>
    /// Writes an <see cref="Instruction"/> to the current <see cref="BasicBlock"/>.
    /// </summary>
    /// <param name="instruction">The <see cref="Instruction"/> to write.</param>
    public void Write(Instruction instruction)
    {
        // If there is no current block, open one
        if (this.currentBlock is null) this.PlaceLabel();
        this.currentBlock!.Instructions.Insert(this.instructionIndex, instruction);
        ++this.instructionIndex;
        if (instruction.IsBranch)
        {
            // Check if there are any instructions to carry over
            var carryOver = (this.currentBlock?.Instructions.Count - this.instructionIndex) ?? 0;
            // TODO
            if (carryOver > 0) throw new NotImplementedException("TODO");
            this.currentBlock = null;
        }
    }

    /// <summary>
    /// Forward-declares a <see cref="Label"/> to a <see cref="BasicBlock"/> that will only come later.
    /// </summary>
    /// <returns>The declared <see cref="Label"/>.</returns>
    public Label DeclareLabel()
    {
        var newBasicBlock = new BasicBlock();
        var label = new Label(newBasicBlock);
        return label;
    }

    /// <summary>
    /// Emplaces a <see cref="Label"/> at the current position.
    /// </summary>
    /// <param name="label">The <see cref="Label"/> to emplace.</param>
    public void PlaceLabel(Label label)
    {
        // Cleck if this is a duplicate placement
        if (this.Procedure.BasicBlocks.Contains(label.Target)) throw new InvalidOperationException("label already placed");
        // Check if we haven't jumped from the previous block
        if (this.currentBlock is not null) this.Jmp(label);
        // Check if there are any instructions to carry over
        var carryOver = (this.currentBlock?.Instructions.Count - this.instructionIndex) ?? 0;
        // TODO
        if (carryOver > 0) throw new NotImplementedException("TODO");
        this.CurrentBlock = label.Target;
        this.Procedure.BasicBlocks.Add(label.Target);
    }

    /// <summary>
    /// Emplaces a new <see cref="Label"/> at the current position.
    /// </summary>
    /// <returns>The emplaced <see cref="Label"/>.</returns>
    public Label PlaceLabel()
    {
        var label = this.DeclareLabel();
        this.PlaceLabel(label);
        return label;
    }

    // Instruction factories ///////////////////////////////////////////////////

    public void Nop() => this.Write(Instruction.Make(InstructionKind.Nop));
    public Value Alloc(Type type)
    {
        if (type == Type.Unit) return Value.Unit.Instance;
        return this.MakeWithRegister(new Type.Ptr(type), target => Instruction.Make(InstructionKind.Alloc, target, type));
    }
    public void Store(Value target, Value src)
    {
        if (src.Type == Type.Unit) return;
        this.Write(Instruction.Make(InstructionKind.Store, target, src));
    }
    public Value Load(Value src)
    {
        if (src.Type == Type.Unit) return Value.Unit.Instance;
        if (src.Type is not Type.Ptr ptr) throw new ArgumentException("can not load from non-pointer value");
        if (ptr.Element == Type.Unit) return Value.Unit.Instance;
        return this.MakeWithRegister(ptr.Element, target => Instruction.Make(InstructionKind.Load, target, src));
    }
    public void Ret(Value value) =>
        this.Write(Instruction.Make(InstructionKind.Ret, value));
    public void Jmp(Label label) => this.Jmp(label.Target);
    public void Jmp(IReadOnlyBasicBlock target) =>
        this.Write(Instruction.Make(InstructionKind.Jmp, target));
    public void JmpIf(Value condition, Label thenLabel, Label elsLabel)
    {
        if (condition.Type != Type.Bool) throw new ArgumentException("condition must be bool");
        this.Write(Instruction.Make(InstructionKind.JmpIf, condition, thenLabel.Target, elsLabel.Target));
    }
    public Value.Reg AddInt(Value a, Value b) =>
        this.MakeWithRegister(a.Type, target => Instruction.Make(InstructionKind.AddInt, target, a, b));
    public Value.Reg SubInt(Value a, Value b) =>
        this.MakeWithRegister(a.Type, target => Instruction.Make(InstructionKind.SubInt, target, a, b));
    public Value.Reg MulInt(Value a, Value b) =>
        this.MakeWithRegister(a.Type, target => Instruction.Make(InstructionKind.MulInt, target, a, b));
    public Value.Reg DivInt(Value a, Value b) =>
        this.MakeWithRegister(a.Type, target => Instruction.Make(InstructionKind.DivInt, target, a, b));
    public Value.Reg RemInt(Value a, Value b) =>
        this.MakeWithRegister(a.Type, target => Instruction.Make(InstructionKind.RemInt, target, a, b));
    public Value.Reg LessInt(Value a, Value b) =>
        this.MakeWithRegister(Type.Bool, target => Instruction.Make(InstructionKind.LessInt, target, a, b));
    public Value.Reg LessEqualInt(Value a, Value b) =>
        this.MakeWithRegister(Type.Bool, target => Instruction.Make(InstructionKind.LessEqualInt, target, a, b));
    public Value.Reg EqualInt(Value a, Value b) =>
        this.MakeWithRegister(Type.Bool, target => Instruction.Make(InstructionKind.EqualInt, target, a, b));
    public Value.Reg NegInt(Value a) =>
        this.MakeWithRegister(a.Type, target => Instruction.Make(InstructionKind.NegInt, target, a));
    public Value.Reg NotBool(Value a) =>
        this.MakeWithRegister(Type.Bool, target => Instruction.Make(InstructionKind.NotBool, target, a));
    public Value.Reg Call(Value called, IList<Value> args)
    {
        if (called.Type is not Type.Proc proc) throw new ArgumentException("can call a non-procedure value");
        return this.MakeWithRegister(proc.Ret, target => Instruction.Make(InstructionKind.Call, target, called, args));
    }

    private Value.Reg MakeWithRegister(Type type, Func<Value.Reg, Instruction> make)
    {
        var result = new Value.Reg(type);
        var instr = make(result);
        this.Write(instr);
        return result;
    }
}
