using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Internal.Symbols;

internal abstract class FieldSymbol : VariableSymbol
{
    public abstract bool IsStatic { get; }

    public override ISymbol ToApiSymbol() => new Api.Semantics.FieldSymbol(this);

    public override void Accept(SymbolVisitor visitor) => visitor.VisitField(this);
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitField(this);
}
