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
/// Extension functions for language servers.
/// </summary>
public static class LanguageServerExtensions
{
    // TODO
    public static (JsonRpc, ILanguageClient) Create(this ILanguageServer languageServer, Stream stream)
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

        // Generate client proxy
        var languageClient = GenerateClientProxy(jsonRpc);

        // TODO
        return (jsonRpc, languageClient);
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
                        var task = this.rpc.InvokeAsync(requestAttr.Method, invocation.Arguments[0]);
                        invocation.ReturnValue = task;
                    }
                    else
                    {
                        var task = this.rpc.InvokeAsync(requestAttr.Method);
                        invocation.ReturnValue = task;
                    }
                }
                else
                {
                    if (invocation.Arguments.Length > 1)
                    {
                        var task = this.rpc.InvokeWithCancellationAsync(
                            requestAttr.Method,
                            new[] { invocation.Arguments[0] },
                            cancellationToken.Value);
                        invocation.ReturnValue = task;
                    }
                    else
                    {
                        var task = this.rpc.InvokeWithCancellationAsync(
                            requestAttr.Method,
                            Array.Empty<object>(),
                            cancellationToken.Value);
                        invocation.ReturnValue = task;
                    }
                }
            }
            if (notificationAttr is not null)
            {
                // It's a notification
                if (invocation.Arguments.Length > 0)
                {
                    var task = this.rpc.NotifyAsync(notificationAttr.Method, invocation.Arguments[0]);
                    invocation.ReturnValue = task;
                }
                else
                {
                    var task = this.rpc.NotifyAsync(notificationAttr.Method);
                    invocation.ReturnValue = task;
                }
            }
        }
    }
}
