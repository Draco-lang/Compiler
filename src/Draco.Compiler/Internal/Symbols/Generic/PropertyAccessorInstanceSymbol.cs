namespace Draco.Compiler.Internal.Symbols.Generic;

/// <summary>
/// Represents a generic instantiated property accessor.
/// It does not necessarily mean that the property accessor itself was generic, it might have been within another generic
/// context (like a generic type definition).
/// </summary>
internal sealed class PropertyAccessorInstanceSymbol(
    Symbol? containingSymbol,
    FunctionSymbol genericDefinition,
    GenericContext context,
    PropertySymbol property)
    : FunctionInstanceSymbol(containingSymbol, genericDefinition, context), IPropertyAccessorSymbol
{
    public PropertySymbol Property { get; } = property;
    public override Api.Semantics.Visibility Visibility => this.GenericDefinition.Visibility;
}
