using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Draco.Lsp.Attributes;
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
        // Create the connection
        var jsonRpc = new JsonRpc(stream);

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

        // Register server methods
        RegisterServerRpcMethods(server, jsonRpc);

        // Done, now we can actually start
        jsonRpc.StartListening();
        await jsonRpc.Completion;
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

    private sealed class LanguageClientInterceptor : IInterceptor
    {
        private readonly JsonRpc rpc;

        public LanguageClientInterceptor(JsonRpc rpc)
        {
            this.rpc = rpc;
        }

        public void Intercept(IInvocation invocation)
        {
            var method = invocation.Method;
            if (method.Name == "get_Connection")
            {
                invocation.ReturnValue = this.rpc;
            }
            else
            {
                invocation.ReturnValue = this.InterceptRpcCall(invocation);
            }
        }

        private object? InterceptRpcCall(IInvocation invocation)
        {
            var method = invocation.Method;
            // Check for attributes
            var requestAttr = method.GetCustomAttribute<RequestAttribute>();
            var notificationAttr = method.GetCustomAttribute<NotificationAttribute>();
            if (requestAttr is not null && notificationAttr is not null)
            {
                throw new InvalidOperationException($"method {method.Name} can not be both a request and notification handler");
            }
            // Call appropriate handler
            if (requestAttr is not null)
            {
                // It's a request
                // Check for cancellation token
                var cancellationToken = null as CancellationToken?;
                if (invocation.Arguments[^1] is CancellationToken)
                {
                    cancellationToken = (CancellationToken)invocation.Arguments[1];
                }
                // Call appropriate variant
                if (cancellationToken is null)
                {
                    if (invocation.Arguments.Length > 0)
                    {
                        return this.rpc.InvokeAsync(requestAttr.Method, invocation.Arguments[0]);
                    }
                    else
                    {
                        return this.rpc.InvokeAsync(requestAttr.Method);
                    }
                }
                else
                {
                    if (invocation.Arguments.Length > 1)
                    {
                        return this.rpc.InvokeWithCancellationAsync(
                            requestAttr.Method,
                            new[] { invocation.Arguments[0] },
                            cancellationToken.Value);
                    }
                    else
                    {
                        return this.rpc.InvokeWithCancellationAsync(
                            requestAttr.Method,
                            Array.Empty<object>(),
                            cancellationToken.Value);
                    }
                }
            }
            if (notificationAttr is not null)
            {
                // It's a notification
                if (invocation.Arguments.Length > 0)
                {
                    return this.rpc.NotifyAsync(notificationAttr.Method, invocation.Arguments[0]);
                }
                else
                {
                    return this.rpc.NotifyAsync(notificationAttr.Method);
                }
            }
            return null;
        }
    }
}
