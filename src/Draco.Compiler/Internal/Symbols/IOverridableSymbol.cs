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

    /// <summary>
    /// Checks if <paramref name="other"/> can be override of this symbol.
    /// </summary>
    /// <param name="other">The symbol that could be override of this symbol.</param>
    /// <returns>True, if <paramref name="other"/> can be override of this symbol, otherwise false.</returns>
    public bool CanBeOverride(IOverridableSymbol other);
}
