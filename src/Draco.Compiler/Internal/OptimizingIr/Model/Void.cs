using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Represents no value.
/// </summary>
internal readonly record struct Void : IOperand
{
    public TypeSymbol Type => WellKnownTypes.Unit;

    public override string ToString() => this.ToOperandString();
    public string ToOperandString() => "unit";
}
