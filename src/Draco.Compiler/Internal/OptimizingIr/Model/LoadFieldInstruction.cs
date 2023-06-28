using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A field access.
/// </summary>
internal sealed class LoadFieldInstruction : InstructionBase
{
    /// <summary>
    /// The register to write the field to.
    /// </summary>
    public Register Target { get; set; }

    /// <summary>
    /// The accessed object.
    /// </summary>
    public IOperand Receiver { get; set; }

    /// <summary>
    /// The accessed member.
    /// </summary>
    public FieldSymbol Member { get; set; }

    public LoadFieldInstruction(Register target, IOperand receiver, FieldSymbol member)
    {
        this.Target = target;
        this.Receiver = receiver;
        this.Member = member;
    }

    public override string ToString() =>
        $"{this.Target.ToOperandString()} := load {this.Receiver.ToOperandString()}.{this.Member.Name}";

    public override LoadFieldInstruction Clone() => new(this.Target, this.Receiver, this.Member);
}
