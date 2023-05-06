using System;
using System.Collections.Generic;

namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Defines an enumeration.
/// </summary>
internal sealed class Enumeration : IDeclaration
{
    public string Name { get; set; } = string.Empty;
    public string? Documentation { get; set; }
    public string? Since { get; set; }
    public bool? Proposed { get; set; }
    public string? Deprecated { get; set; }

    /// <summary>
    /// The type of the elements.
    /// </summary>
    public NamedType Type { get; set; } = null!;

    /// <summary>
    /// The enum values.
    /// </summary>
    public IList<EnumerationEntry> Values { get; set; } = Array.Empty<EnumerationEntry>();

    /// <summary>
    /// Whether the enumeration supports custom values (e.g. values which are not
    /// part of the set defined in `values`). If omitted no custom values are
    /// supported.
    /// </summary>
    public bool? SupportsCustomValues { get; set; }
}
