namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents any symbol that can be overridden.
/// </summary>
/// <typeparam name="TSymbol"></typeparam>
internal interface IOverridableSymbol<TSymbol> where TSymbol : Symbol, ITypedSymbol
{
    /// <summary>
    /// Not null, if this symbol has a different signature than the symbol it overrode.
    /// </summary>
    public TSymbol? ExplicitOverride { get; }
}
