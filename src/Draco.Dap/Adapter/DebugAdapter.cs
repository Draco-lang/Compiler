using System.IO.Pipelines;
using System.Reflection;
using System.Threading.Tasks;

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

        // Done, now we can actually start
        await connection.ListenAsync();
    }

    private static IDebugClient GenerateClientProxy(DebugAdapterConnection connection)
    {
        var proxy = DispatchProxy.Create<IDebugClient, DebugClientProxy>();
        ((DebugClientProxy)(object)proxy).Connection = connection;
        return proxy;
    }
}
