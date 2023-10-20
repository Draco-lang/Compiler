using System;
using System.IO.Pipelines;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Draco.Dap.Attributes;
using Draco.JsonRpc;

namespace Draco.Dap.Adapter;

/// <summary>
/// Debug adapter creation functionality.
/// </summary>
public static class DebugAdapter
{
    /// <summary>
    /// Builds up a connection to a debug adapter.
    /// </summary>
    /// <param name="stream">The duplex communication stream.</param>
    /// <returns>The constructed debug client.</returns>
    public static IDebugClient Connect(IDuplexPipe stream)
    {
        // Create the connection
        var jsonRpc = new DebugAdapterConnection(stream);

        // Generate client proxy
        var debugClient = GenerateClientProxy(jsonRpc);

        // Done
        return debugClient;
    }

    /// <summary>
    /// Starts the message passing between the debug adapter and client.
    /// </summary>
    /// <param name="client">The debug client.</param>
    /// <param name="adapter">The debug adapter.</param>
    /// <returns>The task that completes when the communication is over.</returns>
    public static async Task RunAsync(this IDebugClient client, IDebugAdapter adapter)
    {
        var connection = client.Connection;

        // Register adapter methods
        RegisterAdapterRpcMethods(adapter, connection);

        // Register builtin adapter methods. In the future, we should consider making this extensible in some way.
        var lifecycle = new DebugAdapterLifecycle(adapter, connection);
        RegisterAdapterRpcMethods(lifecycle, connection);

        // Done, now we can actually start
        await connection.ListenAsync();
    }

    private static void RegisterAdapterRpcMethods(object target, IJsonRpcConnection connection)
    {
        // Go through all methods of the adapter and register it
        // NOTE: We go through the interfaces, because interface attributes are not inherited
        var adapterMethods = target
            .GetType()
            .GetInterfaces()
            .SelectMany(i => i.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .Where(m => Attribute.IsDefined(m, typeof(RequestAttribute)) || Attribute.IsDefined(m, typeof(EventAttribute)));

        foreach (var method in adapterMethods)
        {
            connection.AddHandler(new DebugAdapterMethodHandler(method, target));
        }
    }

    private static IDebugClient GenerateClientProxy(DebugAdapterConnection connection)
    {
        var proxy = DispatchProxy.Create<IDebugClient, DebugClientProxy>();
        ((DebugClientProxy)(object)proxy).Connection = connection;
        return proxy;
    }
}
