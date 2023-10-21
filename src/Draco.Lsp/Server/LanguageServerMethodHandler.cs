using System;
using System.Reflection;
using Draco.JsonRpc;
using Draco.Lsp.Attributes;

namespace Draco.Lsp.Server;

internal static class LanguageServerMethodHandler
{
    public static IJsonRpcMethodHandler Create(MethodInfo handlerMethod, object? target)
    {
        var requestAttr = handlerMethod.GetCustomAttribute<RequestAttribute>();
        var notificationAttr = handlerMethod.GetCustomAttribute<NotificationAttribute>();

        if (requestAttr is null && notificationAttr is null)
        {
            throw new ArgumentException("Handler must be marked as either a request or notification handler.", nameof(handlerMethod));
        }

        if (requestAttr is not null && notificationAttr is not null)
        {
            throw new ArgumentException("Handler can not be marked as both a request and notification handler.", nameof(handlerMethod));
        }

        return JsonRpcMethodHandler.Create(
            handlerMethod: handlerMethod,
            target: target,
            methodName: requestAttr?.Method ?? notificationAttr!.Method,
            isRequest: requestAttr is not null,
            isMutating: requestAttr?.Mutating ?? notificationAttr!.Mutating);
    }
}
