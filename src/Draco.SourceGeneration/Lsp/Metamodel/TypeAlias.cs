namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Defines a type alias.
/// (e.g. `type Definition = Location | LocationLink`)
/// </summary>
internal sealed class TypeAlias : IDeclaration
{
    public required string Name { get; set; }
    public string? Documentation { get; set; }
    public string? Since { get; set; }
    public bool? Proposed { get; set; }
    public string? Deprecated { get; set; }

    /// <summary>
    /// The aliased type.
    /// </summary>
    public required Type Type { get; set; }
}
