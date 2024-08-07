using System;

namespace Draco.Lsp.Attributes;

/// <summary>
/// Annotates a server capability interface with the corresponding client capability.
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class ClientCapabilityAttribute(string path) : Attribute
{
    /// <summary>
    /// The capability property path.
    /// </summary>
    public string Path { get; set; } = path;
}
