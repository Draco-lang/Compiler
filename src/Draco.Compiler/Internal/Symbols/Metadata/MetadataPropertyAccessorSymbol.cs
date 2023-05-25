using System.Collections.Immutable;
using System.Reflection.Metadata;
using Draco.Compiler.Internal.Symbols.Generic;

namespace Draco.Compiler.Internal.Symbols.Metadata;

internal class MetadataPropertyAccessorSymbol : MetadataMethodSymbol, IPropertyAccessorSymbol
{
    public PropertySymbol Property { get; }

    public MetadataPropertyAccessorSymbol(Symbol containingSymbol, MethodDefinition definition, PropertySymbol property) : base(containingSymbol, definition)
    {
        this.Property = property;
    }

    public override FunctionSymbol GenericInstantiate(Symbol? containingSymbol, ImmutableArray<TypeSymbol> arguments) =>
        base.GenericInstantiate(containingSymbol, arguments);
    public override FunctionSymbol GenericInstantiate(Symbol? containingSymbol, GenericContext context) =>
        new PropertyAccessorInstanceSymbol(containingSymbol, this, context, this.Property);
}
