using System.Collections.Generic;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Stores a value in a local/global/argument.
/// </summary>
internal sealed class StoreInstruction : InstructionBase
{
    /// <summary>
    /// The symbol to store to.
    /// </summary>
    public Symbol Target { get; set; }

    /// <summary>
    /// The operand to store the value of.
    /// </summary>
    public IOperand Source { get; set; }

    public override IEnumerable<IOperand> Operands => new[] { this.Source };

    public StoreInstruction(Symbol target, IOperand source)
    {
        this.Target = target;
        this.Source = source;
    }

    public override string ToString() => $"store {this.Target.FullName} := {this.Source.ToOperandString()}";

    public override StoreInstruction Clone() => new(this.Target, this.Source);
}
