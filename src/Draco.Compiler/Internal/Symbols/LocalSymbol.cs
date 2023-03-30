using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// A local variable.
/// </summary>
internal abstract partial class LocalSymbol : VariableSymbol
{
    public override ISymbol ToApiSymbol() => new Api.Semantics.LocalSymbol(this);
}
