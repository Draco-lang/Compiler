using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Any variable-like symbol.
/// </summary>
internal abstract partial class VariableSymbol : Symbol, ITypedSymbol
{
    public abstract TypeSymbol Type { get; }

    /// <summary>
    /// True, if this variable is mutable.
    /// </summary>
    public abstract bool IsMutable { get; }
}
