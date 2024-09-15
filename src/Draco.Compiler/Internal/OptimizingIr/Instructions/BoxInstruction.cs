using System.Collections.Generic;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Instructions;

/// <summary>
/// Valuetype element boxing.
/// </summary>
internal sealed class BoxInstruction(
    Register target,
    TypeSymbol boxedType,
    IOperand value) : InstructionBase, IValueInstruction
{
    public override string InstructionKeyword => "box";

    public Register Target { get; set; } = target;

    /// <summary>
    /// The boxed type.
    /// </summary>
    public TypeSymbol BoxedType { get; set; } = boxedType;

    /// <summary>
    /// The boxed value.
    /// </summary>
    public IOperand Value { get; } = value;

    public override IEnumerable<Symbol> StaticOperands => [this.BoxedType];
    public override IEnumerable<IOperand> Operands => [this.Value];

    public override string ToString() =>
        $"{this.Target.ToOperandString()} := {this.InstructionKeyword} {this.Value.ToOperandString()} as {this.BoxedType}";

    public override BoxInstruction Clone() => new(this.Target, this.BoxedType, this.Value);
}
