using System.Collections.Generic;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Loads a value from a local/global/argument.
/// </summary>
internal sealed class LoadInstruction : InstructionBase
{
    /// <summary>
    /// The register to load to.
    /// </summary>
    public Register Target { get; set; }

    /// <summary>
    /// The operand to load from.
    /// </summary>
    public IOperand Source { get; set; }

    public LoadInstruction(Register target, IOperand source)
    {
        this.Target = target;
        this.Source = source;
    }

    public override string ToString() =>
        $"{this.Target.ToOperandString()} := load {this.Source.ToOperandString()}";

    public override LoadInstruction Clone() => new(this.Target, this.Source);
}
