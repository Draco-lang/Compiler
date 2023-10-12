using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Loads the address of some local/global/argument.
/// </summary>
internal sealed class AddressOfInstruction : InstructionBase, IValueInstruction
{
    public string InstructionKeyword => "addressof";

    public Register Target { get; set; }

    /// <summary>
    /// The operand to load from.
    /// </summary>
    public Symbol Source { get; set; }

    public AddressOfInstruction(Register target, Symbol source)
    {
        this.Target = target;
        this.Source = source;
    }

    public override string ToString() =>
        $"{this.Target.ToOperandString()} := {this.InstructionKeyword} {this.Source.FullName}";

    public override AddressOfInstruction Clone() => new(this.Target, this.Source);
}
