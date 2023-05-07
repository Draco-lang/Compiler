namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Represents a literal structure
/// (e.g. `property: { start: uinteger; end: uinteger; }`).
/// </summary>
internal sealed record StructureLiteralType : Type
{
    public override required string Kind { get; set; }

    public StructureLiteral Value { get; set; } = null!;
}
