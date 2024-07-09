using System.Collections.Generic;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A field access.
/// </summary>
internal sealed class LoadFieldInstruction(Register target, IOperand receiver, FieldSymbol member)
    : InstructionBase, IValueInstruction
{
    public override string InstructionKeyword => "loadfield";

    public Register Target { get; set; } = target;

    /// <summary>
    /// The accessed object.
    /// </summary>
    public IOperand Receiver { get; set; } = receiver;

    /// <summary>
    /// The accessed member.
    /// </summary>
    public FieldSymbol Member { get; set; } = member;

    public override IEnumerable<Symbol> StaticOperands => [this.Member];
    public override IEnumerable<IOperand> Operands => [this.Receiver];

    public override string ToString() =>
        $"{this.Target.ToOperandString()} := {this.InstructionKeyword} {this.Receiver.ToOperandString()}.{this.Member.Name}";

    public override LoadFieldInstruction Clone() => new(this.Target, this.Receiver, this.Member);
}
