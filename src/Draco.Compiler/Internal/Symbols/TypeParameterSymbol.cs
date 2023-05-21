using System.Collections.Immutable;
using Draco.Compiler.Internal.Symbols.Generic;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a type parameter in a generic context.
/// </summary>
internal abstract class TypeParameterSymbol : TypeSymbol
{
    public override TypeSymbol GenericInstantiate(Symbol? containingSymbol, ImmutableArray<TypeSymbol> arguments) =>
        (TypeSymbol)base.GenericInstantiate(containingSymbol, arguments);
    public override TypeSymbol GenericInstantiate(Symbol? containingSymbol, GenericContext context) => context.TryGetValue(this, out var type)
        ? type
        : this;

    public override Api.Semantics.ITypeSymbol ToApiSymbol() => new Api.Semantics.TypeParameterSymbol(this);

    public override string ToString() => this.Name;

    public override void Accept(SymbolVisitor visitor) => visitor.VisitTypeParameter(this);
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitTypeParameter(this);
}
