using System.Text;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A symbolic reference.
/// </summary>
/// <param name="Symbol">The symbol referenced.</param>
internal readonly record struct SymbolReference(Symbol Symbol) : IOperand
{
    public TypeSymbol? Type => (this.Symbol as ITypedSymbol)?.Type;

    public override string ToString() => this.ToOperandString();
    public string ToOperandString() =>
        $"{this.Symbol.FullName}{this.Symbol.GenericsToString()}";
}
