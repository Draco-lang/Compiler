using System;
using System.IO.Pipelines;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Protocol;

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
        var jsonRpc = new LspConnection(stream);

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

        // TODO-LSP: Is this extensibility point useful? The default implementation
        // of ILanguageServerLifecycle registers the server capabilities, which doesn't
        // seem like something a user would want to replace. We may need to factor some
        // stuff out for this to make sense.
        if (server is not ILanguageServerLifecycle)
        {
            // Register builtin server methods
            RegisterBuiltinRpcMethods(server, connection);
        }

        // Done, now we can actually start
        await connection.ListenAsync();
    }

    private static void RegisterBuiltinRpcMethods(ILanguageServer server, LspConnection connection)
    {
        var lifecycle = new LanguageServerLifecycle(server, connection);
        RegisterServerRpcMethods(lifecycle, connection);
    }

    private static void RegisterServerRpcMethods(object target, LspConnection connection)
    {
        // Go through all methods of the server and register it
        // NOTE: We go through the interfaces, because interface attributes are not inherited
        var langserverMethods = target
            .GetType()
            .GetInterfaces()
            .SelectMany(i => i.GetMethods(BindingFlags.Public | BindingFlags.Instance));

        foreach (var method in langserverMethods)
        {
            var requestAttr = method.GetCustomAttribute<RequestAttribute>();
            var notificationAttr = method.GetCustomAttribute<NotificationAttribute>();
            if (requestAttr is not null && notificationAttr is not null)
            {
                throw new InvalidOperationException($"method {method.Name} can not be both a request and notification handler");
            }

            var methodName = requestAttr?.Method ?? notificationAttr?.Method ?? "";

            if (requestAttr is not null)
            {
                // It's a request, register it
                connection.AddRpcMethod(new(methodName, method, target, IsRequestHandler: true));
            }
            if (notificationAttr is not null)
            {
                // It's a notification, register it
                connection.AddRpcMethod(new(methodName, method, target, IsRequestHandler: false));
            }
        }
    }

    private static ILanguageClient GenerateClientProxy(LspConnection connection)
    {
        var proxy = DispatchProxy.Create<ILanguageClient, LanguageClientProxy>();
        ((LanguageClientProxy)(object)proxy).Connection = connection;
        return proxy;
    }
}
