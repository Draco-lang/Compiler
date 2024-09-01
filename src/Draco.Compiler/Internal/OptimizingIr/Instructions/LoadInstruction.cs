using System.Collections.Generic;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Instructions;

/// <summary>
/// Loads a value from a local/global/argument.
/// </summary>
internal sealed class LoadInstruction(Register target, Symbol source)
    : InstructionBase, IValueInstruction
{
    public override string InstructionKeyword => "load";

    public Register Target { get; set; } = target;

    /// <summary>
    /// The operand to load from.
    /// </summary>
    public Symbol Source { get; set; } = source;

    public override IEnumerable<Symbol> StaticOperands => [this.Source];

    public override string ToString() =>
        $"{this.Target.ToOperandString()} := {this.InstructionKeyword} {this.Source.FullName}";

    public override LoadInstruction Clone() => new(this.Target, this.Source);
}
