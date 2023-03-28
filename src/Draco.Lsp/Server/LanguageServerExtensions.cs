using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using StreamJsonRpc;

namespace Draco.Lsp.Server;

/// <summary>
/// Extension functions for language servers.
/// </summary>
public static class LanguageServerExtensions
{
    /// <summary>
    /// Runs the language server.
    /// </summary>
    /// <param name="languageServer">The language server to run.</param>
    /// <param name="stream">The duplex communication stream.</param>
    /// <returns>The task that ends when the server is shut down.</returns>
    public static async Task RunAsync(this ILanguageServer languageServer, Stream stream)
    {
        // Create the connection
        var jsonRpc = new JsonRpc(stream);

        // Go through all methods of the server and register it
        // NOTE: We go through the interfaces, because interface attributes are not inherited
        var langserverMethods = languageServer
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
                jsonRpc.AddLocalRpcMethod(method, languageServer, new(requestAttr.Method)
                {
                    UseSingleObjectParameterDeserialization = true,
                });
            }
            if (notificationAttr is not null)
            {
                // It's a notification, register it
                jsonRpc.AddLocalRpcMethod(method, languageServer, new(notificationAttr.Method)
                {
                    UseSingleObjectParameterDeserialization = true,
                });
            }
        }

        // Start the session
        jsonRpc.StartListening();
        await jsonRpc.Completion;
    }
}
