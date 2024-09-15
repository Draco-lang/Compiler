using System.Collections.Generic;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Instructions;

/// <summary>
/// Loads the address of some local/global/argument.
/// </summary>
internal sealed class AddressOfInstruction(Register target, Symbol source)
    : InstructionBase, IValueInstruction
{
    public override string InstructionKeyword => "addressof";

    public Register Target { get; set; } = target;

    /// <summary>
    /// The operand to load from.
    /// </summary>
    public Symbol Source { get; set; } = source;

    public override IEnumerable<Symbol> StaticOperands => [this.Source];

    public override string ToString() =>
        $"{this.Target.ToOperandString()} := {this.InstructionKeyword} {this.Source.FullName}";

    public override AddressOfInstruction Clone() => new(this.Target, this.Source);
}
