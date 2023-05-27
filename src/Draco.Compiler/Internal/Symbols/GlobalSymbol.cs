using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// A global variable.
/// </summary>
internal abstract partial class GlobalSymbol : VariableSymbol, IMemberSymbol
{
    public bool IsStatic => true;

    public override ISymbol ToApiSymbol() => new Api.Semantics.GlobalSymbol(this);

    public override void Accept(SymbolVisitor visitor) => visitor.VisitGlobal(this);
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitGlobal(this);
}
