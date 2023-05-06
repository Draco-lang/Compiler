namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Defines an enumeration entry.
/// </summary>
internal sealed class EnumerationEntry : IDeclaration
{
    public string Name { get; set; } = string.Empty;
    public string? Documentation { get; set; }
    public string? Since { get; set; }
    public bool? Proposed { get; set; }
    public string? Deprecated { get; set; }

    /// <summary>
    /// The value.
    /// </summary>
    public object Value { get; set; } = null!;
}
