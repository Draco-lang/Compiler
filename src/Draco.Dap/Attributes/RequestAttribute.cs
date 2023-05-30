using System;

namespace Draco.Lsp.Attributes;

/// <summary>
/// Annotates a JSON-RPC request.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RequestAttribute : Attribute
{
    /// <summary>
    /// The method being called.
    /// </summary>
    public string Method { get; set; }

    /// <summary>
    /// Whether the request will mutate the debug adapter.
    /// </summary>
    public bool Mutating { get; set; }

    public RequestAttribute(string method)
    {
        this.Method = method;
    }
}
