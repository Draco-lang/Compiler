using System.Collections.Immutable;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols.Generic;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a type definition.
/// </summary>
internal abstract partial class TypeSymbol : Symbol
{
    /// <summary>
    /// True, if this is a type variable, false otherwise.
    /// </summary>
    public virtual bool IsTypeVariable => false;

    /// <summary>
    /// True, if this type is a value-type.
    /// </summary>
    public virtual bool IsValueType => false;

    /// <summary>
    /// True, if this type has everything substituted.
    /// </summary>
    public virtual bool IsGround => true;

    public override TypeSymbol? GenericDefinition => null;

    /// <summary>
    /// Converts this type to a ground type.
    /// </summary>
    /// <param name="solver">The solver to use for type variable substitution.</param>
    /// <returns>The equivalent type without type variables.</returns>
    public virtual TypeSymbol ToGround(ConstraintSolver solver) =>
        throw new System.NotImplementedException();

    public override Symbol GenericInstantiate(Symbol? containingSymbol, ImmutableArray<TypeSymbol> arguments) =>
        (TypeSymbol)base.GenericInstantiate(containingSymbol, arguments);
    public override TypeSymbol GenericInstantiate(Symbol? containingSymbol, GenericContext context) =>
        new TypeInstanceSymbol(containingSymbol, this, context);

    public override Api.Semantics.ISymbol ToApiSymbol() => new Api.Semantics.TypeSymbol(this);

    public override void Accept(SymbolVisitor visitor) => visitor.VisitType(this);
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitType(this);

    public override abstract string ToString();
}
