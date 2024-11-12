using System.Threading;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Symbols.Generic;

/// <summary>
/// Represents a generic instantiated field.
/// The field definition itself is not generic, but the field was within a generic context.
/// </summary>
internal sealed class FieldInstanceSymbol(
    Symbol? containingSymbol,
    FieldSymbol genericDefinition,
    GenericContext context) : FieldSymbol, IGenericInstanceSymbol
{
    public override TypeSymbol Type => LazyInitializer.EnsureInitialized(ref this.type, this.BuildType);
    private TypeSymbol? type;

    public override string Name => this.GenericDefinition.Name;
    public override bool IsMutable => this.GenericDefinition.IsMutable;
    public override bool IsStatic => this.GenericDefinition.IsStatic;
    public override Api.Semantics.Visibility Visibility => this.GenericDefinition.Visibility;
    public override SyntaxNode? DeclaringSyntax => this.GenericDefinition.DeclaringSyntax;

    public override Symbol? ContainingSymbol { get; } = containingSymbol;
    public override FieldSymbol GenericDefinition { get; } = genericDefinition;

    public GenericContext Context { get; } = context;

    private TypeSymbol BuildType() =>
        this.GenericDefinition.Type.GenericInstantiate(this.GenericDefinition.Type.ContainingSymbol, this.Context);
}
