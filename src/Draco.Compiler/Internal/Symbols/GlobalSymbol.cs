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

    public override ISymbol ToApiSymbol() => new Api.Semantics.GlobalSymbol(this);

    public override GlobalSymbol GenericInstantiate(Symbol? containingSymbol, ImmutableArray<TypeSymbol> arguments) =>
        (GlobalSymbol)base.GenericInstantiate(containingSymbol, arguments);
    public override GlobalSymbol GenericInstantiate(Symbol? containingSymbol, GenericContext context) =>
        new GlobalInstanceSymbol(containingSymbol, this, context);

    public override void Accept(SymbolVisitor visitor) => visitor.VisitGlobal(this);
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitGlobal(this);
}
