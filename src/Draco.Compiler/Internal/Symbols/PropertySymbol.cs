using System.Collections.Immutable;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Internal.Symbols.Generic;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a property.
/// </summary>
internal abstract class PropertySymbol : Symbol, ITypedSymbol, IMemberSymbol, IOverridableSymbol
{
    /// <summary>
    /// The getter of this property.
    /// </summary>
    public abstract FunctionSymbol? Getter { get; }

    /// <summary>
    /// The setter of this property.
    /// </summary>
    public abstract FunctionSymbol? Setter { get; }

    public abstract TypeSymbol Type { get; }

    /// <summary>
    /// True, if this property is indexer.
    /// </summary>
    public abstract bool IsIndexer { get; }
    public abstract bool IsStatic { get; }

    public virtual Symbol? Override => null;

    public override bool SignatureEquals(Symbol other)
    {
        if (other is not PropertySymbol prop) return false;
        if (this.Name != prop.Name) return false;
        return SymbolEqualityComparer.Default.Equals(this.Type, prop.Type);
    }

    public bool CanBeOverride(IOverridableSymbol other)
    {
        if (other is not PropertySymbol prop) return false;
        if (this.Name != prop.Name) return false;
        return this.Type.IsBaseTypeOrSameType(prop.Type);
    }

    public override PropertySymbol GenericInstantiate(Symbol? containingSymbol, ImmutableArray<TypeSymbol> arguments) =>
        (PropertySymbol)base.GenericInstantiate(containingSymbol, arguments);
    public override PropertySymbol GenericInstantiate(Symbol? containingSymbol, GenericContext context) =>
        new PropertyInstanceSymbol(containingSymbol, this, context);

    public override IPropertySymbol ToApiSymbol() => new Api.Semantics.PropertySymbol(this);

    public override void Accept(SymbolVisitor visitor) => visitor.VisitProperty(this);
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitProperty(this);
}
