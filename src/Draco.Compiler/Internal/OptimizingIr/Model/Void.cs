using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Represents no value.
/// </summary>
internal readonly record struct Void : IOperand
{
    public TypeSymbol Type => IntrinsicSymbols.Unit;

    public override string ToString() => this.ToOperandString();
    public string ToOperandString() => "unit";
}
