namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Represents an array type (e.g. `TextDocument[]`).
/// </summary>
internal sealed record ArrayType : Type
{
    public override required string Kind { get; set; }

    public required Type Element { get; set; }
}
