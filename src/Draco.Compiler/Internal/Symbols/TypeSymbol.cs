using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Internal.Symbols.Generic;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a type definition.
/// </summary>
internal abstract partial class TypeSymbol : Symbol, IMemberSymbol
{
    /// <summary>
    /// True, if this is a type variable, false otherwise.
    /// </summary>
    public virtual bool IsTypeVariable => false;

    /// <summary>
    /// True, if this is a ground type, meaning there are no type variables or all type variables have been substituted.
    /// </summary>
    public virtual bool IsGroundType => !this.IsGenericInstance || this.GenericArguments.All(t => t.IsGroundType);

    /// <summary>
    /// True, if this type is a value-type.
    /// </summary>
    public virtual bool IsValueType => false;

    /// <summary>
    /// The substituted type of this one, in case this is a type variable.
    /// It's this instance itself, if not a type variable, or not substituted.
    /// </summary>
    public virtual TypeSymbol Substitution => this;

    public virtual IEnumerable<TypeSymbol> BaseTypes => ImmutableArray<TypeSymbol>.Empty;

    public override TypeSymbol? GenericDefinition => null;
    public bool IsStatic => true;

    public override TypeSymbol GenericInstantiate(Symbol? containingSymbol, ImmutableArray<TypeSymbol> arguments) =>
        (TypeSymbol)base.GenericInstantiate(containingSymbol, arguments);
    public override TypeSymbol GenericInstantiate(Symbol? containingSymbol, GenericContext context) =>
        new TypeInstanceSymbol(containingSymbol, this, context);

    public override Api.Semantics.ITypeSymbol ToApiSymbol() => new Api.Semantics.TypeSymbol(this);

    public override void Accept(SymbolVisitor visitor) => visitor.VisitType(this);
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitType(this);

    public override abstract string ToString();
}
