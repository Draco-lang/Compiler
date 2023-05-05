using System;
using System.Collections.Generic;
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
}
