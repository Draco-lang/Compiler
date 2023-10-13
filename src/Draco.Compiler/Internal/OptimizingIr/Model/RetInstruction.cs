using System.Collections.Generic;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Returns from the current procedure.
/// </summary>
internal sealed class RetInstruction : InstructionBase
{
    public override string InstructionKeyword => "ret";

    public override bool IsBranch => true;

    /// <summary>
    /// The returned value.
    /// </summary>
    public IOperand Value { get; set; }

    public override IEnumerable<IOperand> Operands => new[] { this.Value };

    public RetInstruction(IOperand value)
    {
        this.Value = value;
    }

    public override string ToString() => $"{this.InstructionKeyword} {this.Value.ToOperandString()}";

    public override RetInstruction Clone() => new(this.Value);
}
