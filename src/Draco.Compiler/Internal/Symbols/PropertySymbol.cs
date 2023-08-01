using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

    /// <summary>
    /// The parameters of this property.
    /// </summary>
    public ImmutableArray<ParameterSymbol> Parameters
    {
        get
        {
            if (this.Getter is not null) return this.Getter.Parameters;
            if (this.Setter is not null) return this.Setter.Parameters[..^1].ToImmutableArray();
            throw new System.InvalidOperationException();
        }
    }

    /// <summary>
    /// All accessor functions of this property.
    /// </summary>
    public IEnumerable<FunctionSymbol> Accessors
    {
        get
        {
            if (this.Getter is not null) yield return this.Getter;
            if (this.Setter is not null) yield return this.Setter;
        }
    }

    public override bool CanBeShadowedBy(Symbol other)
    {
        if (other is not PropertySymbol prop) return false;
        if (this.Name != prop.Name) return false;
        if (this.Parameters.Length != prop.Parameters.Length) return false;
        for (var i = 0; i < this.Parameters.Length; i++)
        {
            if (!SymbolEqualityComparer.Default.Equals(this.Parameters[i].Type, prop.Parameters[i].Type)) return false;
            if (this.Parameters[i].IsVariadic != prop.Parameters[i].IsVariadic) return false;
        }
        return true;
    }

    public bool CanBeOverriddenBy(IOverridableSymbol other)
    {
        if (other is not PropertySymbol prop) return false;
        if (!this.CanBeShadowedBy(prop)) return false;
        return SymbolEqualityComparer.Default.IsBaseOf(this.Type, prop.Type);
    }

    public override PropertySymbol GenericInstantiate(Symbol? containingSymbol, ImmutableArray<TypeSymbol> arguments) =>
        (PropertySymbol)base.GenericInstantiate(containingSymbol, arguments);
    public override PropertySymbol GenericInstantiate(Symbol? containingSymbol, GenericContext context) =>
        new PropertyInstanceSymbol(containingSymbol, this, context);

    public override IPropertySymbol ToApiSymbol() => new Api.Semantics.PropertySymbol(this);

    public override void Accept(SymbolVisitor visitor) => visitor.VisitProperty(this);
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitProperty(this);
}
