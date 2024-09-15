using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Internal.OptimizingIr.Model;

namespace Draco.Compiler.Internal.OptimizingIr.Instructions;

/// <summary>
/// An array element access.
/// </summary>
internal sealed class LoadElementInstruction(
    Register target,
    IOperand array,
    IEnumerable<IOperand> indices) : InstructionBase, IValueInstruction
{
    public override string InstructionKeyword => "loadelement";

    public Register Target { get; set; } = target;

    /// <summary>
    /// The array to access.
    /// </summary>
    public IOperand Array { get; set; } = array;

    /// <summary>
    /// The element indices.
    /// </summary>
    public IList<IOperand> Indices { get; set; } = indices.ToList();

    public override IEnumerable<IOperand> Operands => this.Indices.Prepend(this.Array);

    public override string ToString() =>
        $"{this.Target.ToOperandString()} := {this.InstructionKeyword} {this.Array.ToOperandString()}[{string.Join(", ", this.Indices.Select(d => d.ToOperandString()))}]";

    public override LoadElementInstruction Clone() => new(this.Target, this.Array, this.Indices);
}
