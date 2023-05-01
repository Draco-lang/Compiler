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
    public string ToOperandString()
    {
        var result = new StringBuilder();
        result.Append(this.Symbol.FullName);
        if (this.Symbol.GenericParameters.Length > 0)
        {
            result.Append('<');
            result.AppendJoin(", ", this.Symbol.GenericParameters);
            result.Append('>');
        }
        if (this.Symbol.GenericArguments.Length > 0)
        {
            result.Append('<');
            result.AppendJoin(", ", this.Symbol.GenericArguments);
            result.Append('>');
        }
        return result.ToString();
    }
}
