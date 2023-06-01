using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Draco.Dap.Adapter;

/// <summary>
/// Intercepts debug client calls.
/// </summary>
// NOTE: Not sealed since DispatchProxy generates a class derived from this at runtime
internal class DebugClientProxy : DispatchProxy
{
    internal DebugAdapterConnection Connection { get; set; } = null!;

    private readonly ConcurrentDictionary<MethodInfo, DebugAdapterMethodHandler> handlers = new();

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
            return this.ProxyRpc(method, args);
        }
    }

    private static readonly MethodInfo SendRequestMethod = typeof(DebugAdapterConnection).GetMethod(nameof(DebugAdapterConnection.SendRequestAsync))!;

    private object? ProxyRpc(MethodInfo method, object?[] arguments)
    {
        var handler = this.handlers.GetOrAdd(method, m => new(m, this));
        var args = handler.HasCancellation ? arguments[..^1] : arguments;

        if (handler.ProducesResponse)
        {
            // It's a request
            // Extract return type
            var returnType = method.ReturnType;

            if (returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                returnType = returnType.GetGenericArguments()[0];
            }

            return SendRequestMethod.MakeGenericMethod(returnType).Invoke(this.Connection, new[] { handler.MethodName, args.SingleOrDefault() });
        }
        else
        {
            // It's an event
            this.Connection.PostEvent(handler.MethodName, args.SingleOrDefault());
            return Task.CompletedTask;
        }
    }
}
