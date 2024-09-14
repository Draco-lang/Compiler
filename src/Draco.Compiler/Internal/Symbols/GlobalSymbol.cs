using System.Collections.Immutable;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Internal.Symbols.Generic;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// A global variable.
/// </summary>
internal abstract partial class GlobalSymbol : VariableSymbol, IMemberSymbol
{
    public bool IsStatic => true;
    public bool IsExplicitImplementation => false;

    // NOTE: Override for covariant return type
    public override GlobalSymbol? GenericDefinition => null;
    public override SymbolKind Kind => SymbolKind.Global;

    /// <summary>
    /// True, if this global is a literal, meaning its value is known at compile-time and has to be inlined.
    /// </summary>
    public virtual bool IsLiteral => false;

    /// <summary>
    /// The literal value of this global, if it is a literal.
    /// </summary>
    public virtual object? LiteralValue => null;

    public override ISymbol ToApiSymbol() => new Api.Semantics.GlobalSymbol(this);

    public override GlobalSymbol GenericInstantiate(Symbol? containingSymbol, ImmutableArray<TypeSymbol> arguments) =>
        (GlobalSymbol)base.GenericInstantiate(containingSymbol, arguments);
    public override GlobalSymbol GenericInstantiate(Symbol? containingSymbol, GenericContext context) =>
        new GlobalInstanceSymbol(containingSymbol, this, context);

    public override void Accept(SymbolVisitor visitor) => visitor.VisitGlobal(this);
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitGlobal(this);
}
