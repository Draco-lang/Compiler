using System.Collections.Generic;

namespace Draco.SourceGeneration.Lsp.CsModel;

/// <summary>
/// An interface declaration.
/// </summary>
public sealed class Interface : Declaration
{
    /// <summary>
    /// The interfaces this interface implements.
    /// </summary>
    public IList<Interface> Interfaces { get; set; } = [];

    /// <summary>
    /// The properties within this interface.
    /// </summary>
    public IList<Property> Properties { get; set; } = [];
}
