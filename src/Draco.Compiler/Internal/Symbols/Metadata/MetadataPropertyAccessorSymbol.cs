using System.Reflection.Metadata;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// Represents an property accessor symbol loaded from metadata.
/// </summary>
internal sealed class MetadataPropertyAccessorSymbol : MetadataMethodSymbol, IPropertyAccessorSymbol
{
    public PropertySymbol Property { get; }

    public MetadataPropertyAccessorSymbol(Symbol containingSymbol, MethodDefinition definition, PropertySymbol property)
        : base(containingSymbol, definition)
    {
        this.Property = property;
    }
}
