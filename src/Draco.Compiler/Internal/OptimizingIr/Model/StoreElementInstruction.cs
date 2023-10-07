using System.Collections.Generic;
using System.Linq;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Stores a value in an array element.
/// </summary>
internal sealed class StoreElementInstruction : InstructionBase
{
    /// <summary>
    /// The array to store to.
    /// </summary>
    public IOperand TargetArray { get; set; }

    /// <summary>
    /// The element indices.
    /// </summary>
    public IList<IOperand> Indices { get; set; } = new List<IOperand>();

    /// <summary>
    /// The operand to store the value of.
    /// </summary>
    public IOperand Source { get; set; }

    public override IEnumerable<IOperand> Operands => this.Indices.Prepend(this.TargetArray).Append(this.Source);

    public StoreElementInstruction(IOperand targetArray, IEnumerable<IOperand> indices, IOperand source)
    {
        this.TargetArray = targetArray;
        this.Indices = indices.ToList();
        this.Source = source;
    }

    public override string ToString() =>
        $"store {this.TargetArray.ToOperandString()}[{string.Join(", ", this.Indices.Select(i => i.ToOperandString()))}] := {this.Source.ToOperandString()}";

    public override StoreElementInstruction Clone() => new(this.TargetArray, this.Indices, this.Source);
}
