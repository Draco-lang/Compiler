namespace Draco.Compiler.Internal.Symbols.Generic;

/// <summary>
/// Represents a generic instantiated property accessor.
/// It does not necessarily mean that the property accessor itself was generic, it might have been within another generic
/// context (like a generic type definition).
/// </summary>
internal sealed class PropertyAccessorInstanceSymbol : FunctionInstanceSymbol, IPropertyAccessorSymbol
{
    public PropertySymbol Property { get; }

    public PropertyAccessorInstanceSymbol(Symbol? containingSymbol, FunctionSymbol genericDefinition, GenericContext context, PropertySymbol property)
        : base(containingSymbol, genericDefinition, context)
    {
        this.Property = property;
    }
}
