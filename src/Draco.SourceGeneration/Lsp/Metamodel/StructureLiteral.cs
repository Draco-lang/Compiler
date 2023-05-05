using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Defines an unnamed structure of an object literal.
/// </summary>
internal sealed class StructureLiteral
{
    /// <summary>
    /// The properties.
    /// </summary>
    public IList<Property> Properties { get; set; } = Array.Empty<Property>();

    /// <summary>
    /// An optional documentation.
    /// </summary>
    public string? Documentation { get; set; }

    /// <summary>
    /// Since when (release number) this structure is
	/// available.Is undefined if not known.
    /// </summary>
    public string? Since { get; set; }

    /// <summary>
    /// Whether this is a proposed structure. If omitted,
	/// the structure is final.
    /// </summary>
    public bool? Proposed { get; set; }

    /// <summary>
    /// Whether the literal is deprecated or not. If deprecated
    /// the property contains the deprecation message.
    /// </summary>
    public string? Deprecated { get; set; }
}
