using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Defines an unnamed structure of an object literal.
/// </summary>
internal sealed class StructureLiteral : IDocumented
{
    public string? Documentation { get; set; }
    public string? Since { get; set; }
    public bool? Proposed { get; set; }
    public string? Deprecated { get; set; }

    /// <summary>
    /// The properties.
    /// </summary>
    public IList<Property> Properties { get; set; } = Array.Empty<Property>();
}
