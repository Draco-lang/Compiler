using System.Collections.Immutable;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Internal.Symbols.Generic;

namespace Draco.Compiler.Internal.Symbols;

internal abstract class PropertySymbol : VariableSymbol
{
    public abstract FunctionSymbol? Getter { get; }
    public abstract FunctionSymbol? Setter { get; }

    public override bool IsMutable => !(this.Setter is null);
    public abstract bool IsIndexer { get; }

    public override PropertySymbol GenericInstantiate(Symbol? containingSymbol, ImmutableArray<TypeSymbol> arguments) =>
    (PropertySymbol)base.GenericInstantiate(containingSymbol, arguments);
    public override PropertySymbol GenericInstantiate(Symbol? containingSymbol, GenericContext context) =>
        new PropertyInstanceSymbol(containingSymbol, this, context);

    public override ISymbol ToApiSymbol() => new Api.Semantics.PropertySymbol(this);

    public override void Accept(SymbolVisitor visitor) => visitor.VisitProperty(this);
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitProperty(this);
}
