namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Includes BaseType, ReferenceType, EnumerationType, MapKeyType.
/// </summary>
internal sealed class NamedType : Type
{
    public override string Kind { get; set; } = string.Empty;

    /// <summary>
    /// The name of the type.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
