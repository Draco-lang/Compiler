using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Internal.Symbols;
internal abstract class PropertySymbol : VariableSymbol
{
    public abstract FunctionSymbol? Getter { get; }
    public abstract FunctionSymbol? Setter { get; }

    public abstract bool IsGettable { get; }
    public abstract bool IsSettable { get; }
    public override bool IsMutable => this.IsSettable;

    public override ISymbol ToApiSymbol() => new Api.Semantics.PropertySymbol(this);

    public override void Accept(SymbolVisitor visitor) => visitor.VisitProperty(this);
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitProperty(this);
}
