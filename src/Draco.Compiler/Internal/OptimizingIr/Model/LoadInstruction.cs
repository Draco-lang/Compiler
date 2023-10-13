using System.Collections.Generic;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Loads a value from a local/global/argument.
/// </summary>
internal sealed class LoadInstruction : InstructionBase, IValueInstruction
{
    public override string InstructionKeyword => "load";

    public Register Target { get; set; }

    /// <summary>
    /// The operand to load from.
    /// </summary>
    public Symbol Source { get; set; }

    public override IEnumerable<Symbol> StaticOperands => new[] { this.Source };

    public LoadInstruction(Register target, Symbol source)
    {
        this.Target = target;
        this.Source = source;
    }

    public override string ToString() =>
        $"{this.Target.ToOperandString()} := {this.InstructionKeyword} {this.Source.FullName}";

    public override LoadInstruction Clone() => new(this.Target, this.Source);
}
