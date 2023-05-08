using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// A local variable.
/// </summary>
internal abstract partial class LocalSymbol : VariableSymbol
{
    public override bool IsStatic => true;

    public override ISymbol ToApiSymbol() => new Api.Semantics.LocalSymbol(this);

    public override void Accept(SymbolVisitor visitor) => visitor.VisitLocal(this);
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitLocal(this);
}
