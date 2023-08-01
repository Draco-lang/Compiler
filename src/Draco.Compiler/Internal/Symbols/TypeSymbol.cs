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

    /// <summary>
    /// The immediate base types of this type.
    /// </summary>
    public virtual ImmutableArray<TypeSymbol> ImmediateBaseTypes => ImmutableArray<TypeSymbol>.Empty;

    /// <summary>
    /// All types that can be considered the base type of this one, including this type itself.
    /// The types are returned in a pre-order manner, starting from this type.
    /// </summary>
    public IEnumerable<TypeSymbol> BaseTypes
    {
        get
        {
            yield return this;
            foreach (var t in this.ImmediateBaseTypes.SelectMany(b => b.BaseTypes)) yield return t;
        }
    }

    /// <summary>
    /// The members defined directly in this type doesn't include members from <see cref="ImmediateBaseTypes"/>.
    /// </summary>
    public virtual IEnumerable<Symbol> DefinedMembers => Enumerable.Empty<Symbol>();

    public sealed override IEnumerable<Symbol> Members => InterlockedUtils.InitializeDefault(ref this.members, this.BuildMembers);
    private ImmutableArray<Symbol> members;

    /// <summary>
    /// All property accessors defined by this type.
    /// </summary>
    public IEnumerable<FunctionSymbol> DefinedPropertyAccessors =>
        this.DefinedMembers.OfType<PropertySymbol>().SelectMany(p => p.Accessors);

    public override TypeSymbol? GenericDefinition => null;
    public bool IsStatic => true;

    public T? GetOverriddenSymbol<T>(T @override)
        where T : Symbol => this.BaseTypes
        .SelectMany(x => x.DefinedMembers)
        .OfType<T>()
        .FirstOrDefault(x => x.SignatureEquals(@override));

    private ImmutableArray<Symbol> BuildMembers()
    {
        var builder = ImmutableArray.CreateBuilder<Symbol>();
        var ignore = new List<Symbol>();
        foreach (var member in this.BaseTypes.Where(x => !x.IsInterface).SelectMany(x => x.DefinedMembers))
        {
            if (ignore.Any(member.SignatureEquals)) continue;
            builder.Add(member);
            ignore.Add(member);
            if (member is not IOverridableSymbol overridable) continue;
            if (overridable.Override is not null) ignore.Add(overridable.Override);
        }
        return builder.ToImmutable();
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
