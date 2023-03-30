using System;
using System.Reflection;
using System.Threading;
using Castle.DynamicProxy;
using Draco.Lsp.Attributes;
using StreamJsonRpc;

namespace Draco.Lsp.Server;

/// <summary>
/// Intercepts language client calls.
/// </summary>
internal sealed class LanguageClientInterceptor : IInterceptor
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
            var cancellationToken = (null as CancellationToken?);
            if (invocation.Arguments[^1] is CancellationToken)
            {
                cancellationToken = (CancellationToken)invocation.Arguments[1];
            }
            // Call appropriate variant
            if (cancellationToken is null)
            {
                if (invocation.Arguments.Length > 0)
                {
                    return this.rpc.InvokeWithParameterObjectAsync(requestAttr.Method, invocation.Arguments[0]);
                }
                else
                {
                    return this.rpc.InvokeWithParameterObjectAsync(requestAttr.Method);
                }
            }
            else
            {
                if (invocation.Arguments.Length > 1)
                {
                    return this.rpc.InvokeWithParameterObjectAsync(
                        requestAttr.Method,
                        invocation.Arguments[0],
                        cancellationToken.Value);
                }
                else
                {
                    return this.rpc.InvokeWithParameterObjectAsync(
                        requestAttr.Method,
                        null,
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
