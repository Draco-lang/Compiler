using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Valuetype element boxing.
/// </summary>
internal sealed class BoxInstruction : InstructionBase, IValueInstruction
{
    public Register Target { get; set; }

    /// <summary>
    /// The boxed type.
    /// </summary>
    public TypeSymbol BoxedType { get; set; }

    /// <summary>
    /// The boxed value.
    /// </summary>
    public IOperand Value { get; }

    public BoxInstruction(Register target, TypeSymbol boxedType, IOperand value)
    {
        this.Target = target;
        this.BoxedType = boxedType;
        this.Value = value;
    }

    public override string ToString() =>
        $"{this.Target.ToOperandString()} := box {this.Value.ToOperandString()} as {this.BoxedType}";

    public override BoxInstruction Clone() => new(this.Target, this.BoxedType, this.Value);
}
