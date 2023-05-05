using System;
using System.Collections.Generic;
using System.Text;

namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Defines the structure of an object literal.
/// </summary>
internal sealed class Structure
{
    /// <summary>
    /// The name of the structure.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
