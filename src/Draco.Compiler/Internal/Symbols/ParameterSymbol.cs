using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a parameter in a function.
/// </summary>
internal abstract partial class ParameterSymbol : LocalSymbol
{
    public override bool IsMutable => false;

    public override IParameterSymbol ToApiSymbol() => new Api.Semantics.ParameterSymbol(this);
}
