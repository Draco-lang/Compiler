using System;

namespace Draco.Dap.Attributes;

/// <summary>
/// Annotates a JSON-RPC event.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class EventAttribute(string method) : Attribute
{
    /// <summary>
    /// The method being called.
    /// </summary>
    public string Method { get; set; } = method;

    /// <summary>
    /// Whether the event will mutate the debug adapter.
    /// </summary>
    public bool Mutating { get; set; }
}
