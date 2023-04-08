using Draco.Compiler.Internal.Types;

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
}
