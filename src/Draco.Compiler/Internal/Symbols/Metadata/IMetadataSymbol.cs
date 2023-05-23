using System.Reflection.Metadata;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// Represents a symbol loaded from metadata.
/// </summary>
internal interface IMetadataSymbol
{
    /// <summary>
    /// The metadata reader that was used to read up this metadata symbol.
    /// </summary>
    public MetadataReader MetadataReader { get; }

    /// <summary>
    /// The metadata assembly of this metadata symbol.
    /// </summary>
    public MetadataAssemblySymbol Assembly { get; }

    /// <summary>
    /// The metadata name used for referencing this symbol.
    /// </summary>
    public string MetadataName { get; }
}
