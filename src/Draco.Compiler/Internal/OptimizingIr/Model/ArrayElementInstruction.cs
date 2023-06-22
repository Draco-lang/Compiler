using System.Collections.Generic;
using System.Linq;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// An array element access.
/// </summary>
internal sealed class ArrayElementInstruction : InstructionBase
{
    public override IEnumerable<IOperand> Operands =>
        new IOperand[] { this.Target, this.Array }.Concat(this.Indices);

    /// <summary>
    /// The register to write the array element to.
    /// </summary>
    public Register Target { get; set; }

    /// <summary>
    /// The array to access.
    /// </summary>
    public IOperand Array { get; set; }

    /// <summary>
    /// The element indices.
    /// </summary>
    public IList<IOperand> Indices { get; set; } = new List<IOperand>();

    public ArrayElementInstruction(Register target, IOperand array, IEnumerable<IOperand> indices)
    {
        this.Target = target;
        this.Array = array;
        this.Indices = indices.ToList();
    }

    public override string ToString() =>
        $"{this.Target.ToOperandString()} := {this.Array.ToOperandString()}[{string.Join(", ", this.Indices.Select(d => d.ToOperandString()))}]";

    public override ArrayElementInstruction Clone() => new(this.Target, this.Array, this.Indices);
}
