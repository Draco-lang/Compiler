namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Includes StringLiteralType, IntegerLiteralType, BooleanLiteralType.
/// </summary>
internal sealed class LiteralType : Type
{
    public override string Kind { get; set; } = string.Empty;

    public object Value { get; set; } = null!;
}
