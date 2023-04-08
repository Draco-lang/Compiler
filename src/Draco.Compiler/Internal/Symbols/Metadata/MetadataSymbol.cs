using System.Reflection;
using System.Reflection.Metadata;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// Utilities for reading up metadata symbols.
/// </summary>
internal static class MetadataSymbol
{
    /// <summary>
    /// Attributes of a static class.
    /// </summary>
    private static readonly TypeAttributes StaticClassAttributes =
        TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Class;

    public static Symbol ToSymbol(
        Symbol containingSymbol,
        TypeDefinition type,
        MetadataReader metadataReader) => type.Attributes.HasFlag(StaticClassAttributes)
        ? new MetadataStaticClassSymbol(containingSymbol, type, metadataReader)
        : new MetadataTypeSymbol(containingSymbol, type, metadataReader);
}
