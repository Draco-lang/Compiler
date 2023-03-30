using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a label.
/// </summary>
internal abstract partial class LabelSymbol : Symbol
{
    public override ISymbol ToApiSymbol() => new Api.Semantics.LabelSymbol(this);

    public override void Accept(SymbolVisitor visitor) => visitor.VisitLabel(this);
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitLabel(this);
}
