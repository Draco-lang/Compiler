using System;
using System.Collections.Generic;
using System.Text;

namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Represents a LSP notification.
/// </summary>
internal sealed class Notification
{
    /// <summary>
    /// The notification's method name.
    /// </summary>
    public string Method { get; set; } = string.Empty;
}
