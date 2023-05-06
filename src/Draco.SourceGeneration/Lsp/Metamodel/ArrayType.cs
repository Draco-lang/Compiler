namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Represents an array type (e.g. `TextDocument[]`).
/// </summary>
internal sealed class ArrayType : Type
{
    public override string Kind { get; set; } = string.Empty;

    public Type Element { get; set; } = null!;
}
