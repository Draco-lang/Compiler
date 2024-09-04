using System.Reflection.Metadata;
using Draco.Compiler.Api;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// Represents an property accessor symbol loaded from metadata.
/// </summary>
internal sealed class MetadataPropertyAccessorSymbol(
    Symbol containingSymbol,
    MethodDefinition definition,
    PropertySymbol property) : MetadataMethodSymbol(containingSymbol, definition), IPropertyAccessorSymbol
{
    public PropertySymbol Property { get; } = property;
}
