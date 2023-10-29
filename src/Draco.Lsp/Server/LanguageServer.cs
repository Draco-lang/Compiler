using System;
using System.IO.Pipelines;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Draco.JsonRpc;
using Draco.Lsp.Attributes;

namespace Draco.Lsp.Server;

/// <summary>
/// Language server creation functionality.
/// </summary>
public static class LanguageServer
{
    /// <summary>
    /// Builds up a connection to a language server.
    /// </summary>
    /// <param name="stream">The duplex communication stream.</param>
    /// <returns>The constructed language client.</returns>
    public static ILanguageClient Connect(IDuplexPipe stream)
    {
        // Create the connection
        var jsonRpc = new LanguageServerConnection(stream);

        // Generate client proxy
        var languageClient = GenerateClientProxy(jsonRpc);

        // Done
        return languageClient;
    }

    /// <summary>
    /// Starts the message passing between the language server and client.
    /// </summary>
    /// <param name="client">The language client.</param>
    /// <param name="server">The language server.</param>
    /// <returns>The task that completes when the communication is over.</returns>
    public static async Task RunAsync(this ILanguageClient client, ILanguageServer server)
    {
        var connection = client.Connection;

        // Register server methods
        RegisterServerRpcMethods(server, connection);

        // Register builtin server methods. In the future, we should consider making this extensible in some way.
        var lifecycle = new LanguageServerLifecycle(server, connection);
        RegisterServerRpcMethods(lifecycle, connection);

        // Done, now we can actually start
        await connection.ListenAsync();
    }

    private static void RegisterServerRpcMethods(object target, IJsonRpcConnection connection)
    {
        // Go through all methods of the server and register it
        // NOTE: We go through the interfaces, because interface attributes are not inherited
        var langserverMethods = target
            .GetType()
            .GetInterfaces()
            .SelectMany(i => i.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .Where(m => Attribute.IsDefined(m, typeof(RequestAttribute)) || Attribute.IsDefined(m, typeof(NotificationAttribute)));

        foreach (var method in langserverMethods)
        {
            connection.AddHandler(LanguageServerMethodHandler.Create(method, target));
        }
    }

    private static ILanguageClient GenerateClientProxy(LanguageServerConnection connection)
    {
        var proxy = DispatchProxy.Create<ILanguageClient, LanguageClientProxy>();
        ((LanguageClientProxy)(object)proxy).Connection = connection;
        return proxy;
    }
}
