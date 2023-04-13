using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// An array instantiation.
/// </summary>
internal sealed class NewArrayInstruction : InstructionBase
{
    public override IEnumerable<IOperand> Operands => this.Dimensions.Prepend(this.Target);

    /// <summary>
    /// The register to write the instantiated array to.
    /// </summary>
    public Register Target { get; set; }

    /// <summary>
    /// The array element type.
    /// </summary>
    public TypeSymbol ElementType { get; set; }

    /// <summary>
    /// The dimension sizes.
    /// </summary>
    public IList<IOperand> Dimensions { get; set; } = new List<IOperand>();

    public NewArrayInstruction(Register target, TypeSymbol elementType, IEnumerable<IOperand> dimensions)
    {
        this.Target = target;
        this.ElementType = elementType;
        this.Dimensions = dimensions.ToList();
    }

    public override string ToString() =>
        $"{this.Target.ToOperandString()} := new {this.ElementType}[{string.Join(", ", this.Dimensions.Select(d => d.ToOperandString()))}]";

    public override NewArrayInstruction Clone() => new(this.Target, this.ElementType, this.Dimensions);
}
