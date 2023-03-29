using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;
using Draco.Lsp.Serialization;
using Newtonsoft.Json.Converters;
using StreamJsonRpc;

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
    public static ILanguageClient Connect(Stream stream)
    {
        // Create an RPC message handler with the custom JSON converters
        var messageFormatter = new JsonMessageFormatter();
        messageFormatter.JsonSerializer.Converters.Add(new EnumValueConverter());
        var messageHandler = new HeaderDelimitedMessageHandler(stream, messageFormatter);

        // Create the connection
        var jsonRpc = new JsonRpc(messageHandler);

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
        var jsonRpc = client.Connection;

        // Register builtin server methods
        RegisterBuiltinRpcMethods(server, jsonRpc);
        // Register server methods
        RegisterServerRpcMethods(server, jsonRpc);

        // Done, now we can actually start
        jsonRpc.StartListening();
        await jsonRpc.Completion;
    }

    private static void RegisterBuiltinRpcMethods(ILanguageServer server, JsonRpc jsonRpc)
    {
        var lifecycle = new LanguageServerLifecycle(server, jsonRpc);
        jsonRpc.AddLocalRpcTarget(lifecycle);
    }

    private static void RegisterServerRpcMethods(ILanguageServer server, JsonRpc jsonRpc)
    {
        // Go through all methods of the server and register it
        // NOTE: We go through the interfaces, because interface attributes are not inherited
        var langserverMethods = server
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

            if (requestAttr is not null)
            {
                // It's a request, register it
                jsonRpc.AddLocalRpcMethod(method, server, new(requestAttr.Method)
                {
                    UseSingleObjectParameterDeserialization = true,
                });
            }
            if (notificationAttr is not null)
            {
                // It's a notification, register it
                jsonRpc.AddLocalRpcMethod(method, server, new(notificationAttr.Method)
                {
                    UseSingleObjectParameterDeserialization = true,
                });
            }
        }
    }

    private static ILanguageClient GenerateClientProxy(JsonRpc rpc)
    {
        var generator = new ProxyGenerator();
        return generator.CreateInterfaceProxyWithoutTarget<ILanguageClient>(new LanguageClientInterceptor(rpc));
    }
}
