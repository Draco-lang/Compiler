namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Includes StringLiteralType, IntegerLiteralType, BooleanLiteralType.
/// </summary>
internal sealed record LiteralType : Type
{
    public override required string Kind { get; set; }

    public required object Value { get; set; }
}
