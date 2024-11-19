using System.Collections.Immutable;
using System.Text;
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

    /// <summary>
    /// True, if this is a 'this' (receiver) parameter.
    /// </summary>
    public virtual bool IsThis => false;

    public override bool IsMutable => false;

    // NOTE: Override for covariant return type
    public override ParameterSymbol? GenericDefinition => null;
    public override SymbolKind Kind => SymbolKind.Parameter;

    public override ParameterSymbol GenericInstantiate(Symbol? containingSymbol, ImmutableArray<TypeSymbol> arguments) =>
        (ParameterSymbol)base.GenericInstantiate(containingSymbol, arguments);
    public override ParameterSymbol GenericInstantiate(Symbol? containingSymbol, GenericContext context) =>
        new ParameterInstanceSymbol((FunctionSymbol)containingSymbol!, this, context);

    public override IParameterSymbol ToApiSymbol() => new Api.Semantics.ParameterSymbol(this);

    public override void Accept(SymbolVisitor visitor) => visitor.VisitParameter(this);
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitParameter(this);

    public override string ToString()
    {
        var result = new StringBuilder();
        if (this.IsVariadic) result.Append("...");
        if (!string.IsNullOrWhiteSpace(this.Name)) result.Append($"{this.Name}: ");
        result.Append(this.Type);
        return result.ToString();
    }
}
