namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Represents no value.
/// </summary>
internal readonly record struct Void : IOperand
{
    public Type? Type => IntrinsicTypes.Unit;

    public override string ToString() => this.ToOperandString();
    public string ToOperandString() => "unit";
}
