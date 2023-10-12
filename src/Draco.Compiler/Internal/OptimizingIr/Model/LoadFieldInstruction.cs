using System.Collections.Generic;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A field access.
/// </summary>
internal sealed class LoadFieldInstruction : InstructionBase, IValueInstruction
{
    public override string InstructionKeyword => "loadfield";

    public Register Target { get; set; }

    /// <summary>
    /// The accessed object.
    /// </summary>
    public IOperand Receiver { get; set; }

    /// <summary>
    /// The accessed member.
    /// </summary>
    public FieldSymbol Member { get; set; }

    public override IEnumerable<IOperand> Operands => new[] { this.Receiver };

    public LoadFieldInstruction(Register target, IOperand receiver, FieldSymbol member)
    {
        this.Target = target;
        this.Receiver = receiver;
        this.Member = member;
    }

    public override string ToString() =>
        $"{this.Target.ToOperandString()} := {this.InstructionKeyword} {this.Receiver.ToOperandString()}.{this.Member.Name}";

    public override LoadFieldInstruction Clone() => new(this.Target, this.Receiver, this.Member);
}
