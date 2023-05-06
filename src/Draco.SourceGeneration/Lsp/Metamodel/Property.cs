namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Represents an object property.
/// </summary>
internal sealed class Property : IDeclaration
{
    /// <summary>
    /// The property name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The type of the property.
    /// </summary>
    public Type Type { get; set; } = null!;

    /// <summary>
    /// Whether the property is optional. If
    /// omitted, the property is mandatory.
    /// </summary>
    public bool? Optional { get; set; }

    /// <summary>
    /// An optional documentation.
    /// </summary>
    public string? Documentation { get; set; }

    /// <summary>
    /// Since when (release number) this property is
    /// available.Is undefined if not known.
    /// </summary>
    public string? Since { get; set; }

    /// <summary>
    /// Whether this is a proposed property. If omitted,
    /// the structure is final.
    /// </summary>
    public bool? Proposed { get; set; }

    /// <summary>
    /// Whether the property is deprecated or not. If deprecated
    /// the property contains the deprecation message.
    /// </summary>
    public string? Deprecated { get; set; }

    public bool IsOptional => this.Optional ?? false;
}
