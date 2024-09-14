using System.Collections.Immutable;
using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// Not a true symbol, represents a set of function overloads.
/// </summary>
internal sealed class FunctionGroupSymbol : Symbol
{
    /// <summary>
    /// The candidate functions in the overload set.
    /// </summary>
    public ImmutableArray<FunctionSymbol> Functions { get; }

    public override Symbol? ContainingSymbol => throw new System.NotSupportedException();
    public override string Name => this.Functions[0].Name;
    public override SymbolKind Kind => SymbolKind.FunctionGroup;

    public FunctionGroupSymbol(ImmutableArray<FunctionSymbol> functions)
    {
        if (functions.Length == 0)
        {
            throw new System.ArgumentException("overloads must contain at least one function", nameof(functions));
        }
        this.Functions = functions;
    }

    public override ISymbol ToApiSymbol() => new Api.Semantics.FunctionGroupSymbol(this);

    public override void Accept(SymbolVisitor visitor) => throw new System.NotSupportedException();
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => throw new System.NotSupportedException();
}
