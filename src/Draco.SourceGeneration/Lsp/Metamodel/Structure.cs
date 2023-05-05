using System;
using System.Collections.Generic;
using System.ComponentModel;
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

    /// <summary>
    /// Structures extended from. This structures form
	/// a polymorphic type hierarchy.
    /// </summary>
    public IList<Type>? Extends { get; set; }

    /// <summary>
    /// Structures to mix in. The properties of these
	/// structures are `copied` into this structure.
    /// Mixins don't form a polymorphic type hierarchy in
	/// LSP.
    /// </summary>
    public IList<Type>? Mixins { get; set; }

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
    /// Whether the structure is deprecated or not. If deprecated
	/// the property contains the deprecation message.
    /// </summary>
    public string? Deprecated { get; set; }
}
