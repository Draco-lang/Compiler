using System;
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
    /// True, if this type is interface.
    /// </summary>
    public virtual bool IsInterface => false;

    /// <summary>
    /// The substituted type of this one, in case this is a type variable.
    /// It's this instance itself, if not a type variable, or not substituted.
    /// </summary>
    public virtual TypeSymbol Substitution => this;

    public virtual ImmutableArray<TypeSymbol> BaseTypes => ImmutableArray<TypeSymbol>.Empty;

    /// <summary>
    /// The members defined directly in this type doesn't include members from <see cref="BaseTypes"/>.
    /// </summary>
    public virtual IEnumerable<Symbol> DefinedMembers => Enumerable.Empty<Symbol>();

    // TODO: Filter out overrides and interface implementation
    public sealed override IEnumerable<Symbol> Members => InterlockedUtils.InitializeDefault(ref this.members, () => this.BuildMembers());
    private ImmutableArray<Symbol> members;

    public override TypeSymbol? GenericDefinition => null;
    public bool IsStatic => true;

    private ImmutableArray<Symbol> BuildMembers()
    {
        var builder = ImmutableArray.CreateBuilder<Symbol>();
        var ignore = new List<Symbol>();
        foreach (var member in this.DefinedMembers)
        {
            builder.Add(member);
            if (member is IOverridableSymbol overridable && overridable.ExplicitOverride is not null) ignore.Add(overridable.ExplicitOverride);
            else ignore.Add(member);
        }
        Recurse(this);
        return builder.ToImmutable();

        void Recurse(TypeSymbol current)
        {
            foreach (var baseType in current.BaseTypes.Where(x => !x.IsInterface))
            {
                foreach (var member in baseType.DefinedMembers)
                {
                    if (ignore.Any(member.SignatureEquals)) continue;
                    builder.Add(member);
                    if (member is IOverridableSymbol overridable && overridable.ExplicitOverride is not null) ignore.Add(overridable.ExplicitOverride);
                    else ignore.Add(member);
                }
                Recurse(baseType);
            }
        }
    }

    public override TypeSymbol GenericInstantiate(Symbol? containingSymbol, ImmutableArray<TypeSymbol> arguments) =>
        (TypeSymbol)base.GenericInstantiate(containingSymbol, arguments);
    public override TypeSymbol GenericInstantiate(Symbol? containingSymbol, GenericContext context) =>
        new TypeInstanceSymbol(containingSymbol, this, context);

    public override Api.Semantics.ITypeSymbol ToApiSymbol() => new Api.Semantics.TypeSymbol(this);

    public override void Accept(SymbolVisitor visitor) => visitor.VisitType(this);
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitType(this);

    public override abstract string ToString();
}
