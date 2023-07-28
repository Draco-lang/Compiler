using System;

namespace Draco.Lsp.Attributes;

/// <summary>
/// Annotates a server capability property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ServerCapabilityAttribute : Attribute
{
    /// <summary>
    /// The capability's property name.
    /// </summary>
    public string Property { get; set; }

    public ServerCapabilityAttribute(string property)
    {
        this.Property = property;
    }
}
