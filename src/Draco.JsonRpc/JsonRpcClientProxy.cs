using System.Collections.Concurrent;
using System.Reflection;

namespace Draco.JsonRpc;

/// <summary>
/// A proxy type for a JSON-RPC client.
/// </summary>
public abstract class JsonRpcClientProxy : DispatchProxy
{
    /// <summary>
    /// The connection of the client.
    /// </summary>
    public IJsonRpcConnection Connection { get; set; } = null!;

    private readonly ConcurrentDictionary<MethodInfo, IJsonRpcMethodHandler> handlers = new();

    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(method, nameof(method));
        ArgumentNullException.ThrowIfNull(args, nameof(args));

        if (method.Name == $"get_{nameof(IJsonRpcClient.Connection)}")
        {
            return this.Connection;
        }
        else
        {
            return this.ProxyRpc(method, args);
        }
    }

    private static readonly MethodInfo SendRequestMethod = typeof(IJsonRpcConnection)
        .GetMethod(nameof(IJsonRpcConnection.SendRequestAsync))!;

    private object? ProxyRpc(MethodInfo method, object?[] arguments)
    {
        var handler = this.handlers.GetOrAdd(method, m => this.CreateHandler(m));

        if (handler.IsRequest)
        {
            // Build up args
            var args = new List<object?>();
            // Method name
            args.Add(handler.MethodName);
            // Parameter
            if (handler.AcceptsParams)
            {
                args.Add(arguments[0]);
            }
            else
            {
                args.Add(null);
            }
            // CT
            if (handler.SupportsCancellation)
            {
                args.Add(arguments[1]!);
            }
            else
            {
                args.Add(CancellationToken.None);
            }

            // Extract return type
            var returnType = method.ReturnType;

            if (returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                returnType = returnType.GetGenericArguments()[0];
            }

            return SendRequestMethod
                .MakeGenericMethod(returnType)
                .Invoke(this.Connection, args.ToArray());
        }
        else
        {
            // It's a notification
            return this.Connection.SendNotificationAsync(handler.MethodName, arguments.SingleOrDefault());
        }
    }

    /// <summary>
    /// Constructs a handler for the given method.
    /// </summary>
    /// <param name="method">The method to construct the handler for.</param>
    /// <returns>The constructed handler.</returns>
    protected abstract IJsonRpcMethodHandler CreateHandler(MethodInfo method);
}
