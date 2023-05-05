using System;
using System.Collections.Generic;
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
}
