using System.Collections.Generic;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Instructions;

/// <summary>
/// Stores a value in a local/global/argument.
/// </summary>
internal sealed class StoreInstruction(Symbol target, IOperand source) : InstructionBase
{
    public override string InstructionKeyword => "store";

    /// <summary>
    /// The symbol to store to.
    /// </summary>
    public Symbol Target { get; set; } = target;

    /// <summary>
    /// The operand to store the value of.
    /// </summary>
    public IOperand Source { get; set; } = source;

    public override IEnumerable<Symbol> StaticOperands => [this.Target];
    public override IEnumerable<IOperand> Operands => [this.Source];

    public override string ToString() => $"{this.InstructionKeyword} {this.Target.FullName} := {this.Source.ToOperandString()}";

    public override StoreInstruction Clone() => new(this.Target, this.Source);
}
