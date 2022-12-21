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
        if (this.currentBasicBlock is null) throw new InvalidOperationException("The current Basic Block could not be inferred");
        this.currentBasicBlock.Instructions.Add(instruction);
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
    public void Ret(Value value) => this.Write(Instruction.Ret(value));
    public void Jmp(Label label) => this.Write(Instruction.Jmp(label.Target));
    public void JmpIf(Value condition, Label thenLabel, Label elsLabel) =>
        this.Write(Instruction.JmpIf(condition, thenLabel.Target, elsLabel.Target));
    public Value.Register AddInt(Value a, Value b) => this.MakeWithRegister(target => Instruction.AddInt(target, a, b));

    private Value.Register MakeWithRegister(Func<Value.Register, Instruction> make)
    {
        var result = new Value.Register();
        var instr = make(result);
        this.Write(instr);
        return result;
    }
}
