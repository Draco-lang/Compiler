using System;

namespace Draco.Dap.Attributes;

/// <summary>
/// Annotates a JSON-RPC request.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RequestAttribute(string method) : Attribute
{
    /// <summary>
    /// The method being called.
    /// </summary>
    public string Method { get; set; } = method;

    /// <summary>
    /// Whether the request will mutate the debug adapter.
    /// </summary>
    public bool Mutating { get; set; }
}
