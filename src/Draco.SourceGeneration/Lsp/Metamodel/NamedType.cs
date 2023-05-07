namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Includes BaseType, ReferenceType, EnumerationType, MapKeyType.
/// </summary>
internal sealed record NamedType : Type
{
    public override required string Kind { get; set; }

    /// <summary>
    /// The name of the type.
    /// </summary>
    public required string Name { get; set; }
}
