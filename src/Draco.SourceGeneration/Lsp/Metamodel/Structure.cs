using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Defines the structure of an object literal.
/// </summary>
internal sealed class Structure : IDeclaration
{
    public string Name { get; set; } = string.Empty;
    public string? Documentation { get; set; }
    public string? Since { get; set; }
    public bool? Proposed { get; set; }
    public string? Deprecated { get; set; }

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
}
