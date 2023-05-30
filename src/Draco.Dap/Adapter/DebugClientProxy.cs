using System;
using System.Reflection;

namespace Draco.Dap.Adapter;

/// <summary>
/// Intercepts debug client calls.
/// </summary>
// NOTE: Not sealed since DispatchProxy generates a class derived from this at runtime
internal class DebugClientProxy : DispatchProxy
{
    internal DebugAdapterConnection Connection { get; set; } = null!;

    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(method, nameof(method));
        ArgumentNullException.ThrowIfNull(args, nameof(args));

        if (method.Name == $"get_{nameof(IDebugClient.Connection)}")
        {
            return this.Connection;
        }
        else
        {
            // TODO: RPC proxy calls
            return null;
        }
    }
}
