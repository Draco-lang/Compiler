using System.Collections.Generic;
using System.Linq;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// An array element access.
/// </summary>
internal sealed class LoadElementInstruction : InstructionBase, IValueInstruction
{
    public Register Target { get; set; }

    /// <summary>
    /// The array to access.
    /// </summary>
    public IOperand Array { get; set; }

    /// <summary>
    /// The element indices.
    /// </summary>
    public IList<IOperand> Indices { get; set; } = new List<IOperand>();

    public override IEnumerable<IOperand> Operands => this.Indices.Prepend(this.Array);

    public LoadElementInstruction(Register target, IOperand array, IEnumerable<IOperand> indices)
    {
        this.Target = target;
        this.Array = array;
        this.Indices = indices.ToList();
    }

    public override string ToString() =>
        $"{this.Target.ToOperandString()} := load {this.Array.ToOperandString()}[{string.Join(", ", this.Indices.Select(d => d.ToOperandString()))}]";

    public override LoadElementInstruction Clone() => new(this.Target, this.Array, this.Indices);
}
