namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents any member symbol.
/// </summary>
internal interface IMemberSymbol
{
    /// <summary>
    /// Specifying if given symbol is static.
    /// </summary>
    public bool IsStatic { get; }
}
