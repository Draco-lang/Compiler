using System.Collections.Generic;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Returns from the current procedure.
/// </summary>
internal sealed class RetInstruction(IOperand value) : InstructionBase
{
    public override string InstructionKeyword => "ret";

    public override bool IsBranch => true;

    /// <summary>
    /// The returned value.
    /// </summary>
    public IOperand Value { get; set; } = value;

    public override IEnumerable<IOperand> Operands => [this.Value];

    public override string ToString() => $"{this.InstructionKeyword} {this.Value.ToOperandString()}";

    public override RetInstruction Clone() => new(this.Value);
}
