namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents any symbol that can be overridden.
/// </summary>
internal interface IOverridableSymbol
{
    /// <summary>
    /// Represents the overridden symbol.
    /// Null, if this symbol doesn't override anything.
    /// </summary>
    public Symbol? Override { get; }
}
