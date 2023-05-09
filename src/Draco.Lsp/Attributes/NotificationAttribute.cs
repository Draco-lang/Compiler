using System;

namespace Draco.Lsp.Attributes;

/// <summary>
/// Annotates a JSON-RPC notification.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class NotificationAttribute : Attribute
{
    /// <summary>
    /// The method being called.
    /// </summary>
    public string Method { get; set; }

    /// <summary>
    /// Whether the notification will mutate the workspace.
    /// </summary>
    public bool Mutating { get; set; }

    public NotificationAttribute(string method)
    {
        this.Method = method;
    }
}
