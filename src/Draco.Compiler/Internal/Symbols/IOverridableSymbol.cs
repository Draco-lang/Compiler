namespace Draco.Compiler.Internal.Symbols;

internal interface IOverridableSymbol<TSymbol> where TSymbol : Symbol, ITypedSymbol
{
    public TSymbol? Overridden { get; }
}
