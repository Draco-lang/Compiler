using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A compiler intrinsic.
/// </summary>
/// <param name="Symbol">The corresponding intrinsic symbol.</param>
internal readonly record struct Intrinsic(Symbol Symbol) : IOperand
{
    public Type? Type => (this.Symbol as ITypedSymbol)?.Type;

    public override string ToString() => this.ToOperandString();
    public string ToOperandString() => $"[intrinsic {this.Symbol.Name}]";
}
