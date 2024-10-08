using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Internal.OptimizingIr.Model;

namespace Draco.Compiler.Internal.OptimizingIr.Instructions;

/// <summary>
/// Stores a value in an array element.
/// </summary>
internal sealed class StoreElementInstruction(
    IOperand targetArray,
    IEnumerable<IOperand> indices,
    IOperand source) : InstructionBase
{
    public override string InstructionKeyword => "storeelement";

    /// <summary>
    /// The array to store to.
    /// </summary>
    public IOperand TargetArray { get; set; } = targetArray;

    /// <summary>
    /// The element indices.
    /// </summary>
    public IList<IOperand> Indices { get; set; } = indices.ToList();

    /// <summary>
    /// The operand to store the value of.
    /// </summary>
    public IOperand Source { get; set; } = source;

    public override IEnumerable<IOperand> Operands => this.Indices.Prepend(this.TargetArray).Append(this.Source);

    public override string ToString() =>
        $"{this.InstructionKeyword} {this.TargetArray.ToOperandString()}[{string.Join(", ", this.Indices.Select(i => i.ToOperandString()))}] := {this.Source.ToOperandString()}";

    public override StoreElementInstruction Clone() => new(this.TargetArray, this.Indices, this.Source);
}
