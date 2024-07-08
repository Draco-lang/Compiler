using System;

namespace Draco.Lsp.Attributes;

/// <summary>
/// Annotates a server capability property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ServerCapabilityAttribute(string property) : Attribute
{
    /// <summary>
    /// The capability's property name.
    /// </summary>
    public string Property { get; set; } = property;
}
