using System;

namespace Draco.Dap.Attributes;

/// <summary>
/// Annotates a JSON-RPC event.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class EventAttribute : Attribute
{
    /// <summary>
    /// The method being called.
    /// </summary>
    public string Method { get; set; }

    /// <summary>
    /// Whether the event will mutate the debug adapter.
    /// </summary>
    public bool Mutating { get; set; }

    public EventAttribute(string method)
    {
        this.Method = method;
    }
}
