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
/// Helper to write <see cref="Instruction"/>s to a <see cref="Procedure"/>.
/// </summary>
internal sealed class InstructionWriter
{
    /// <summary>
    /// True, if the last instruction was some kind of branching.
    /// </summary>
    public bool EndsInBranch => this.currentBasicBlock is null;

    private readonly Procedure procedure;
    private BasicBlock? currentBasicBlock;

    public InstructionWriter(Procedure procedure)
    {
        this.procedure = procedure;
        this.currentBasicBlock = procedure.Entry;
    }

    /// <summary>
    /// Writes an <see cref="Instruction"/> to the current <see cref="BasicBlock"/>.
    /// </summary>
    /// <param name="instruction">The <see cref="Instruction"/> to write.</param>
    public void Write(Instruction instruction)
    {
        // If there is no current block, open one
        if (this.currentBasicBlock is null) this.PlaceLabel();
        this.currentBasicBlock!.Instructions.Add(instruction);
        if (instruction.IsBranch) this.currentBasicBlock = null;
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
        if (this.procedure.BasicBlocks.Contains(label.Target)) throw new InvalidOperationException("label already placed");
        // Check if we haven't jumped from the previous block
        if (this.currentBasicBlock is not null) this.Write(Instruction.Jmp(label.Target));
        this.currentBasicBlock = label.Target;
        this.procedure.BasicBlocks.Add(label.Target);
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

    public void Nop() => this.Write(Instruction.Nop());
    public Value Alloc(Type type)
    {
        if (type == Type.Unit) return Value.Unit.Instance;
        return this.MakeWithRegister(new Type.Ptr(type), target => Instruction.Alloc(target, type));
    }
    public void Store(Value target, Value src)
    {
        if (src.Type == Type.Unit) return;
        this.Write(Instruction.Store(target, src));
    }
    public Value Load(Value src)
    {
        if (src.Type == Type.Unit) return Value.Unit.Instance;
        if (src.Type is not Type.Ptr ptr) throw new ArgumentException("can not load from non-pointer value");
        if (ptr.Element == Type.Unit) return Value.Unit.Instance;
        return this.MakeWithRegister(ptr.Element, target => Instruction.Load(target, src));
    }
    public void Ret(Value value) => this.Write(Instruction.Ret(value));
    public void Jmp(Label label) => this.Write(Instruction.Jmp(label.Target));
    public void JmpIf(Value condition, Label thenLabel, Label elsLabel)
    {
        if (condition.Type != Type.Bool) throw new ArgumentException("condition must be bool");
        this.Write(Instruction.JmpIf(condition, thenLabel.Target, elsLabel.Target));
    }
    public Value.Register AddInt(Value a, Value b) =>
        this.MakeWithRegister(a.Type, target => Instruction.AddInt(target, a, b));
    public Value.Register SubInt(Value a, Value b) =>
        this.MakeWithRegister(a.Type, target => Instruction.SubInt(target, a, b));
    public Value.Register MulInt(Value a, Value b) =>
        this.MakeWithRegister(a.Type, target => Instruction.MulInt(target, a, b));
    public Value.Register DivInt(Value a, Value b) =>
        this.MakeWithRegister(a.Type, target => Instruction.DivInt(target, a, b));
    public Value.Register RemInt(Value a, Value b) =>
        this.MakeWithRegister(a.Type, target => Instruction.RemInt(target, a, b));
    public Value.Register LessInt(Value a, Value b) =>
        this.MakeWithRegister(Type.Bool, target => Instruction.LessInt(target, a, b));
    public Value.Register LessEqualInt(Value a, Value b) =>
        this.MakeWithRegister(Type.Bool, target => Instruction.LessEqualInt(target, a, b));
    public Value.Register EqualInt(Value a, Value b) =>
        this.MakeWithRegister(Type.Bool, target => Instruction.EqualInt(target, a, b));
    public Value.Register NegInt(Value a) =>
        this.MakeWithRegister(a.Type, target => Instruction.NegInt(target, a));
    public Value.Register NotBool(Value a) =>
        this.MakeWithRegister(Type.Bool, target => Instruction.NotBool(target, a));

    private Value.Register MakeWithRegister(Type type, Func<Value.Register, Instruction> make)
    {
        var result = new Value.Register(type);
        var instr = make(result);
        this.Write(instr);
        return result;
    }
}
