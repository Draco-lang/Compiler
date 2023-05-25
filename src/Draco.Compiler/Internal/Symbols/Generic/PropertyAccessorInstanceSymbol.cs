namespace Draco.Compiler.Internal.Symbols.Generic;

internal sealed class PropertyAccessorInstanceSymbol : FunctionInstanceSymbol, IPropertyAccessorSymbol
{
    public PropertySymbol Property { get; }
    public PropertyAccessorInstanceSymbol(Symbol? containingSymbol, FunctionSymbol genericDefinition, GenericContext context, PropertySymbol property) : base(containingSymbol, genericDefinition, context)
    {
        this.Property = property;
    }
}
