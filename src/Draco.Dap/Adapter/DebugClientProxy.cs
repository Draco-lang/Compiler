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
        return null;
    }
}
