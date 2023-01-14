using System;
using System.Collections.Immutable;

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
        var newBasicBlock = new BasicBlock(this.Procedure);
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

    public void Nop() => this.Write(Instruction.Make0(InstructionKind.Nop));
    public void Store(Global target, Value src)
    {
        if (src.Type == Type.Unit) return;
        this.Write(Instruction.Make2(InstructionKind.Store, target, src));
    }
    public void Store(Local target, Value src)
    {
        if (src.Type == Type.Unit) return;
        this.Write(Instruction.Make2(InstructionKind.Store, target, src));
    }
    public Value Load(Global src)
    {
        if (src.Type == Type.Unit) return Value.Unit.Instance;
        return this.MakeWithRegister(src.Type, r => Instruction.Make1(InstructionKind.Load, r, src));
    }
    public Value Load(Local src)
    {
        if (src.Type == Type.Unit) return Value.Unit.Instance;
        return this.MakeWithRegister(src.Type, r => Instruction.Make1(InstructionKind.Load, r, src));
    }
    public void Ret() => this.Ret(Value.Unit.Instance);
    public void Ret(Value value) =>
        this.Write(Instruction.Make1(InstructionKind.Ret, value));
    public void Jmp(Label label) => this.Jmp(label.Target);
    public void Jmp(IReadOnlyBasicBlock target) =>
        this.Write(Instruction.Make1(InstructionKind.Jmp, target));
    public void JmpIf(Value condition, Label thenLabel, Label elsLabel) =>
        this.JmpIf(condition, thenLabel.Target, elsLabel.Target);
    public void JmpIf(Value condition, IReadOnlyBasicBlock thenTarget, IReadOnlyBasicBlock elsTarget)
    {
        if (condition.Type != Type.Bool) throw new ArgumentException("condition must be bool");
        this.Write(Instruction.Make3(InstructionKind.JmpIf, condition, thenTarget, elsTarget));
    }
    public Value.Reg Add(Value a, Value b) =>
        this.MakeWithRegister(a.Type, target => Instruction.Make2(InstructionKind.Add, target, a, b));
    public Value.Reg Sub(Value a, Value b) =>
        this.MakeWithRegister(a.Type, target => Instruction.Make2(InstructionKind.Sub, target, a, b));
    public Value.Reg Mul(Value a, Value b) =>
        this.MakeWithRegister(a.Type, target => Instruction.Make2(InstructionKind.Mul, target, a, b));
    public Value.Reg Div(Value a, Value b) =>
        this.MakeWithRegister(a.Type, target => Instruction.Make2(InstructionKind.Div, target, a, b));
    public Value.Reg Rem(Value a, Value b) =>
        this.MakeWithRegister(a.Type, target => Instruction.Make2(InstructionKind.Rem, target, a, b));
    public Value.Reg Less(Value a, Value b) =>
        this.MakeWithRegister(Type.Bool, target => Instruction.Make2(InstructionKind.Less, target, a, b));
    public Value.Reg Equal(Value a, Value b) =>
        this.MakeWithRegister(Type.Bool, target => Instruction.Make2(InstructionKind.Equal, target, a, b));
    public Value.Reg Neg(Value a) =>
        this.MakeWithRegister(a.Type, target => Instruction.Make1(InstructionKind.Neg, target, a));
    public Value.Reg Call(Value called, ImmutableArray<Value> args) =>
        this.Call(called, new ArgumentList(args));
    public Value.Reg Call(Value called, ArgumentList args)
    {
        if (called.Type is not Type.Proc proc) throw new ArgumentException("can call a non-procedure value");
        return this.MakeWithRegister(proc.Ret, target => Instruction.Make2(InstructionKind.Call, target, called, args));
    }

    private Value.Reg MakeWithRegister(Type type, Func<Value.Reg, Instruction> make)
    {
        var result = type == Type.Unit ? null! : new Value.Reg(type);
        var instr = make(result);
        this.Write(instr);
        return result;
    }
}
