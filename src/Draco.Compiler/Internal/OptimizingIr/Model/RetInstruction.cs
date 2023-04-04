using System.Collections.Generic;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Returns from the current procedure.
/// </summary>
internal sealed class RetInstruction : InstructionBase
{
    public override bool IsBranch => true;
    public override IEnumerable<IOperand> Operands => new[] { this.Value };

    /// <summary>
    /// The returned value.
    /// </summary>
    public IOperand Value { get; set; }

    public RetInstruction(IOperand value)
    {
        this.Value = value;
    }

    public override RetInstruction Clone() => new(this.Value);
}
