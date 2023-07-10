namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents any symbol that can be overridden.
/// </summary>
internal interface IOverridableSymbol
{
    /// <summary>
    /// Represents the explicitly overridden symbol.
    /// Not null, if this symbol has a different signature than the symbol it overrode.
    /// </summary>
    public Symbol? ExplicitOverride { get; }
}
