namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Defines an enumeration entry.
/// </summary>
internal sealed class EnumerationEntry : IDeclaration
{
    public required string Name { get; set; }
    public string? Documentation { get; set; }
    public string? Since { get; set; }
    public bool? Proposed { get; set; }
    public string? Deprecated { get; set; }

    /// <summary>
    /// The value.
    /// </summary>
    public required object Value { get; set; }
}
