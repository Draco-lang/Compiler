namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Represents no value.
/// </summary>
internal readonly record struct Void : IOperand
{
    public override string ToString() => this.ToOperandString();
    public string ToOperandString() => "unit";
}
