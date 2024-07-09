using System;

namespace Draco.Lsp.Attributes;

/// <summary>
/// Annotates a JSON-RPC notification.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class NotificationAttribute(string method) : Attribute
{
    /// <summary>
    /// The method being called.
    /// </summary>
    public string Method { get; set; } = method;

    /// <summary>
    /// Whether the notification will mutate the workspace.
    /// </summary>
    public bool Mutating { get; set; }
}
