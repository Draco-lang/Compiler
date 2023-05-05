using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Defines a type alias.
/// (e.g. `type Definition = Location | LocationLink`)
/// </summary>
internal sealed class TypeAlias
{
    /// <summary>
    /// The name of the type alias.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The aliased type.
    /// </summary>
    public Type Type { get; set; } = null!;

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
    /// Whether this is a proposed type alias. If omitted,
    /// the type alias is final.
    /// </summary>
    public bool? Proposed { get; set; }

    /// <summary>
    /// Whether the type alias is deprecated or not. If deprecated
	/// the property contains the deprecation message.
    /// </summary>
    public string? Deprecated { get; set; }
}
