using System.Collections.Generic;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Instructions;

/// <summary>
/// Stores a value in a field.
/// </summary>
internal sealed class StoreFieldInstruction(
    IOperand receiver,
    FieldSymbol member,
    IOperand source) : InstructionBase
{
    public override string InstructionKeyword => "storefield";

    /// <summary>
    /// The accessed object.
    /// </summary>
    public IOperand Receiver { get; set; } = receiver;

    /// <summary>
    /// The accessed member.
    /// </summary>
    public FieldSymbol Member { get; set; } = member;

    /// <summary>
    /// The operand to store the value of.
    /// </summary>
    public IOperand Source { get; set; } = source;

    public override IEnumerable<Symbol> StaticOperands => [this.Member];
    public override IEnumerable<IOperand> Operands => [this.Receiver, this.Source];

    public override string ToString() =>
        $"{this.InstructionKeyword} {this.Receiver.ToOperandString()}.{this.Member.Name} := {this.Source.ToOperandString()}";

    public override StoreFieldInstruction Clone() => new(this.Receiver, this.Member, this.Source);
}
