namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Represents a TS documented declaration in the metamodel.
/// </summary>
internal interface IDeclaration : IDocumented
{
    /// <summary>
    /// The name of the declaration element.
    /// </summary>
    public string Name { get; set; }
}
