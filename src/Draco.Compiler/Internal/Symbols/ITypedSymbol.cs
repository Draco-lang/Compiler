namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents any symbol that has a type associated with it.
/// </summary>
internal interface ITypedSymbol
{
    /// <summary>
    /// The type of value the symbol references.
    /// </summary>
    public TypeSymbol Type { get; }

    /// <summary>
    /// Specifing if given symbol is static.
    /// </summary>
    public bool IsStatic { get; }
}
