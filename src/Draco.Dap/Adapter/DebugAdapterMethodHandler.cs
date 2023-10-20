using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Draco.Dap.Attributes;
using Draco.JsonRpc;

namespace Draco.Dap.Adapter;

internal static class DebugAdapterMethodHandler
{
    public static IJsonRpcMethodHandler Create(MethodInfo handlerMethod, object? target)
    {
        var requestAttr = handlerMethod.GetCustomAttribute<RequestAttribute>();
        var eventAttr = handlerMethod.GetCustomAttribute<EventAttribute>();

        if (requestAttr is null && eventAttr is null)
        {
            throw new ArgumentException("Handler must be marked as either a request or event handler.", nameof(handlerMethod));
        }

        if (requestAttr is not null && eventAttr is not null)
        {
            throw new ArgumentException("Handler can not be marked as both a request and event handler.", nameof(handlerMethod));
        }

        return JsonRpcMethodHandler.Create(
            handlerMethod: handlerMethod,
            target: target,
            methodName: requestAttr?.Method ?? eventAttr!.Method,
            isRequest: requestAttr is not null,
            isMutating: requestAttr?.Mutating ?? eventAttr!.Mutating);
    }
}
