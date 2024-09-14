using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Alias for a symbol.
/// </summary>
internal abstract class AliasSymbol : Symbol, IMemberSymbol
{
    public bool IsStatic => true;
    public bool IsExplicitImplementation => false;
    public override SymbolKind Kind => SymbolKind.Alias;

    /// <summary>
    /// The symbol being aliased.
    /// </summary>
    public abstract Symbol Substitution { get; }

    /// <summary>
    /// Retrieves the full resolution of the alias.
    /// </summary>
    public Symbol FullResolution
    {
        get
        {
            var symbol = this.Substitution;
            while (symbol is AliasSymbol alias) symbol = alias.Substitution;
            return symbol;
        }
    }

    public override void Accept(SymbolVisitor visitor) => visitor.VisitAlias(this);
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitAlias(this);
    public override ISymbol ToApiSymbol() => new Api.Semantics.AliasSymbol(this);
}
