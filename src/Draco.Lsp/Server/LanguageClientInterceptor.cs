using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Draco.Lsp.Attributes;
using Newtonsoft.Json.Linq;
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
            // Extract return type
            var taskReturnType = method.ReturnType;
            var returnType = taskReturnType.GetGenericArguments()[0];
            // Check for cancellation token and args
            var (args, cancellationToken) = invocation.Arguments[^1] is CancellationToken ct
                ? (invocation.Arguments.Length > 1 ? (invocation.Arguments[0], ct) : (null, ct))
                : (invocation.Arguments.Length > 0 ? (invocation.Arguments[0], default) : (null, default));
            // Build the generic method
            var methodInfo = typeof(JsonRpc)
                .GetMethod(nameof(JsonRpc.InvokeWithParameterObjectAsync), 1, new[] { typeof(string), typeof(object), typeof(CancellationToken) });
            var methodInstance = methodInfo!.MakeGenericMethod(returnType);
            // Call it
            return methodInstance.Invoke(this.rpc, new[] { requestAttr.Method, args, cancellationToken });
        }
        if (notificationAttr is not null)
        {
            // It's a notification
            var args = invocation.Arguments.FirstOrDefault();
            return this.rpc.NotifyWithParameterObjectAsync(notificationAttr.Method, args);
        }
        return null;
    }
}
