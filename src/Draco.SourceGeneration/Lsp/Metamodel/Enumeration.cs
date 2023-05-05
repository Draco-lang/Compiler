using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Defines an enumeration.
/// </summary>
internal sealed class Enumeration
{
    /// <summary>
    /// The name of the enumeration.
    /// </summary>
    public string Name { get; set; } = string.Empty;

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

    /// <summary>
    /// An optional documentation.
    /// </summary>
    public string? Documentation { get; set; }

    /// <summary>
    /// Since when (release number) this enumeration is
	/// available.Is undefined if not known.
    /// </summary>
    public string? Since { get; set; }

    /// <summary>
    /// Whether this is a proposed enumeration. If omitted,
	/// the enumeration is final.
    /// </summary>
    public bool? Proposed { get; set; }

    /// <summary>
    /// Whether the enumeration is deprecated or not. If deprecated
	/// the property contains the deprecation message.
    /// </summary>
    public string? Deprecated { get; set; }
}
