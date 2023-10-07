using System.Collections.Generic;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Loads a value from a local/global/argument.
/// </summary>
internal sealed class LoadInstruction : InstructionBase, IValueInstruction
{
    public Register Target { get; set; }

    /// <summary>
    /// The operand to load from.
    /// </summary>
    public IOperand Source { get; set; }

    public override IEnumerable<IOperand> Operands => new[] { this.Source };

    public LoadInstruction(Register target, IOperand source)
    {
        this.Target = target;
        this.Source = source;
    }

    public override string ToString() =>
        $"{this.Target.ToOperandString()} := load {this.Source.ToOperandString()}";

    public override LoadInstruction Clone() => new(this.Target, this.Source);
}
