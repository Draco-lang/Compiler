using System.Collections.Immutable;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Internal.Symbols.Generic;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a parameter in a function.
/// </summary>
internal abstract partial class ParameterSymbol : LocalSymbol
{
    public override abstract FunctionSymbol ContainingSymbol { get; }

    /// <summary>
    /// True, if this a variadic parameter.
    /// </summary>
    public virtual bool IsVariadic => false;

    public override bool IsMutable => false;

    // NOTE: Override for covariant return type
    public override ParameterSymbol? GenericDefinition => null;

    public override ParameterSymbol GenericInstantiate(Symbol? containingSymbol, ImmutableArray<TypeSymbol> arguments) =>
        (ParameterSymbol)base.GenericInstantiate(containingSymbol, arguments);
    public override ParameterSymbol GenericInstantiate(Symbol? containingSymbol, GenericContext context) =>
        new ParameterInstanceSymbol((FunctionSymbol)containingSymbol!, this, context);

    public override IParameterSymbol ToApiSymbol() => new Api.Semantics.ParameterSymbol(this);

    public override void Accept(SymbolVisitor visitor) => visitor.VisitParameter(this);
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitParameter(this);

    public override string ToString() => string.IsNullOrWhiteSpace(this.Name)
        ? this.Type.ToString()
        : $"{this.Name}: {this.Type}";
}
