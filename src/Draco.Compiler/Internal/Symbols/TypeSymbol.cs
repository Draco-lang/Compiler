using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a type definition.
/// </summary>
internal abstract partial class TypeSymbol : Symbol
{
    /// <summary>
    /// The defined type.
    /// </summary>
    public abstract Type Type { get; }

    public override Api.Semantics.ISymbol ToApiSymbol() => new Api.Semantics.TypeSymbol(this);
}
