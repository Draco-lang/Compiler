using System;
using System.Collections.Generic;
using System.Text;

namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Represents an object property.
/// </summary>
internal sealed class Property
{
    /// <summary>
    /// The property name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The type of the property.
    /// </summary>
    public Type Type { get; set; } = null!;
}
