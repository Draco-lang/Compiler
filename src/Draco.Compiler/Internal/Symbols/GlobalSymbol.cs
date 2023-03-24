using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// A global variable.
/// </summary>
internal abstract partial class GlobalSymbol : VariableSymbol
{
    public override ISymbol ToApiSymbol() => new Api.Semantics.GlobalSymbol(this);
}
