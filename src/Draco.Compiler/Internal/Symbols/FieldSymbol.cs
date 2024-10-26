using System.Collections.Immutable;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Internal.Symbols.Generic;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a field either on a module-level (global variable) or on a type-level (instance field).
/// </summary>
internal abstract class FieldSymbol : VariableSymbol, IMemberSymbol
{
    public virtual bool IsStatic => this.ContainingSymbol is ModuleSymbol;
    public bool IsExplicitImplementation => false;

    // NOTE: Override for covariant return type
    public override FieldSymbol? GenericDefinition => null;
    public override SymbolKind Kind => SymbolKind.Field;

    /// <summary>
    /// True, if this global is a literal, meaning its value is known at compile-time and has to be inlined.
    /// </summary>
    public virtual bool IsLiteral => false;

    /// <summary>
    /// The literal value of this global, if it is a literal.
    /// </summary>
    public virtual object? LiteralValue => null;

    public override FieldSymbol GenericInstantiate(Symbol? containingSymbol, ImmutableArray<TypeSymbol> arguments) =>
        (FieldSymbol)base.GenericInstantiate(containingSymbol, arguments);
    public override FieldSymbol GenericInstantiate(Symbol? containingSymbol, GenericContext context) =>
        new FieldInstanceSymbol(containingSymbol, this, context);

    public override ISymbol ToApiSymbol() => new Api.Semantics.FieldSymbol(this);

    public override void Accept(SymbolVisitor visitor) => visitor.VisitField(this);
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitField(this);
}
