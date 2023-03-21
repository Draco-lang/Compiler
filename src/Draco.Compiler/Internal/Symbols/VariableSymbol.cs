using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Any variable-like symbol.
/// </summary>
internal abstract partial class VariableSymbol : Symbol, ITypedSymbol
{
    /// <summary>
    /// The type of the local.
    /// </summary>
    public abstract Type Type { get; }

    /// <summary>
    /// True, if this local is mutable.
    /// </summary>
    public abstract bool IsMutable { get; }
}
