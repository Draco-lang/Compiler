using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// An array instantiation.
/// </summary>
internal sealed class NewArrayInstruction(
    Register target,
    TypeSymbol elementType,
    IEnumerable<IOperand> dimensions) : InstructionBase, IValueInstruction
{
    public override string InstructionKeyword => "newarray";

    public Register Target { get; set; } = target;

    /// <summary>
    /// The array element type.
    /// </summary>
    public TypeSymbol ElementType { get; set; } = elementType;

    /// <summary>
    /// The dimension sizes.
    /// </summary>
    public IList<IOperand> Dimensions { get; set; } = dimensions.ToList();

    public override IEnumerable<Symbol> StaticOperands => [this.ElementType];
    public override IEnumerable<IOperand> Operands => this.Dimensions;

    public override string ToString() =>
        $"{this.Target.ToOperandString()} := {this.InstructionKeyword} {this.ElementType}[{string.Join(", ", this.Dimensions.Select(d => d.ToOperandString()))}]";

    public override NewArrayInstruction Clone() => new(this.Target, this.ElementType, this.Dimensions);
}
