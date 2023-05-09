using System;
using System.Collections.Generic;

namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Defines an enumeration.
/// </summary>
internal sealed class Enumeration : IDeclaration
{
    public required string Name { get; set; }
    public string? Documentation { get; set; }
    public string? Since { get; set; }
    public bool? Proposed { get; set; }
    public string? Deprecated { get; set; }

    /// <summary>
    /// The type of the elements.
    /// </summary>
    public required NamedType Type { get; set; }

    /// <summary>
    /// The enum values.
    /// </summary>
    public required IList<EnumerationEntry> Values { get; set; }

    /// <summary>
    /// Whether the enumeration supports custom values (e.g. values which are not
    /// part of the set defined in `values`). If omitted no custom values are
    /// supported.
    /// </summary>
    public bool? SupportsCustomValues { get; set; }
}
