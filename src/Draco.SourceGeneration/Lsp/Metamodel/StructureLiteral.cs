using System;
using System.Collections.Generic;

namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Defines an unnamed structure of an object literal.
/// </summary>
internal sealed record class StructureLiteral : IDocumented
{
    public string? Documentation { get; set; }
    public string? Since { get; set; }
    public bool? Proposed { get; set; }
    public string? Deprecated { get; set; }

    /// <summary>
    /// The properties.
    /// </summary>
    public required EquatableArray<Property> Properties { get; set; }
}
