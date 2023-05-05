using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Defines an enumeration entry.
/// </summary>
internal sealed class EnumerationEntry
{
    /// <summary>
    /// The name of the enum item.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The value.
    /// </summary>
    public object Value { get; set; } = null!;

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
    /// Whether this is a proposed enumeration entry. If omitted,
	/// the enumeration entry is final.
    /// </summary>
    public bool? Proposed { get; set; }

    /// <summary>
    /// Whether the enum entry is deprecated or not. If deprecated
	/// the property contains the deprecation message.
    /// </summary>
    public string? Deprecated { get; set; }
}
