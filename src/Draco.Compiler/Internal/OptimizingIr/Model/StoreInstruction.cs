using System.Collections.Generic;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Stores a value in a local/global/argument.
/// </summary>
internal sealed class StoreInstruction : InstructionBase
{
    public override IEnumerable<IOperand> Operands => new[] { this.Target, this.Source };

    /// <summary>
    /// The operand to store to.
    /// </summary>
    public IOperand Target { get; set; }

    /// <summary>
    /// The operand to store the value of.
    /// </summary>
    public IOperand Source { get; set; }

    public StoreInstruction(IOperand target, IOperand source)
    {
        this.Target = target;
        this.Source = source;
    }

    public override StoreInstruction Clone() => new(this.Target, this.Source);
}
