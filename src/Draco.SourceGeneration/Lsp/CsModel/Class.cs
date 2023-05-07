using System.Collections.Generic;

namespace Draco.SourceGeneration.Lsp.CsModel;

/// <summary>
/// A class declaration.
/// </summary>
public sealed class Class : Declaration
{
    /// <summary>
    /// The parent of this class in terms of containment, not inheritance.
    /// </summary>
    public Class? Parent { get; set; }

    /// <summary>
    /// The interfaces this class implements.
    /// </summary>
    public IList<Interface> Interfaces { get; set; } = new List<Interface>();

    /// <summary>
    /// The declarations this class has nested within it.
    /// </summary>
    public IList<Declaration> NestedDeclarations { get; set; } = new List<Declaration>();

    /// <summary>
    /// The properties within this class.
    /// </summary>
    public IList<Property> Properties { get; set; } = new List<Property>();
}
