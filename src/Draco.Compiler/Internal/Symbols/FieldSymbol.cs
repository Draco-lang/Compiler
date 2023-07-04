using System.Collections.Immutable;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Internal.Symbols.Generic;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a field.
/// </summary>
internal abstract class FieldSymbol : VariableSymbol, IMemberSymbol, IOverridableSymbol<FieldSymbol>
{
    public abstract bool IsStatic { get; }

    public virtual FieldSymbol? Overridden => null;

    public override FieldSymbol GenericInstantiate(Symbol? containingSymbol, ImmutableArray<TypeSymbol> arguments) =>
        (FieldSymbol)base.GenericInstantiate(containingSymbol, arguments);
    public override FieldSymbol GenericInstantiate(Symbol? containingSymbol, GenericContext context) =>
        new FieldInstanceSymbol(containingSymbol, this, context);

    public override ISymbol ToApiSymbol() => new Api.Semantics.FieldSymbol(this);

    public override void Accept(SymbolVisitor visitor) => visitor.VisitField(this);
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitField(this);
}
