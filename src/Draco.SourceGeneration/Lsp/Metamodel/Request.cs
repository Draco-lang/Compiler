using System;
using System.Collections.Generic;
using System.Text;

namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Represents a LSP request.
/// </summary>
internal sealed class Request
{
    /// <summary>
    /// The request's method name.
    /// </summary>
    public string Method { get; set; } = string.Empty;
}
