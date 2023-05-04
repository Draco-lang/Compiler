using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Draco.Lsp.Attributes;
using Draco.Lsp.Protocol;


namespace Draco.Lsp.Server;

/// <summary>
/// Intercepts language client calls.
/// </summary>
internal class LanguageClientProxy : DispatchProxy
{
    internal LspConnection Connection { get; set; } = null!;

    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(method, nameof(method));
        ArgumentNullException.ThrowIfNull(args, nameof(args));

        if (method.Name == $"get_{nameof(ILanguageClient.Connection)}")
        {
            return this.Connection;
        }
        else
        {
            return this.ProxyRpc(method, args);
        }
    }

    private static readonly MethodInfo SendRequestMethod = typeof(LspConnection).GetMethod(nameof(LspConnection.SendRequestAsync))!;

    private object? ProxyRpc(MethodInfo method, object?[] arguments)
    {
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
            var returnType = method.ReturnType;

            if (returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                returnType = returnType.GetGenericArguments()[0];
            }

            // Check for cancellation token and args
            var (args, cancellationToken) = arguments[^1] is CancellationToken ct
                ? (arguments.Length > 1 ? (arguments[0], ct) : (null, ct))
                : (arguments.Length > 0 ? (arguments[0], default) : (null, default));

            return SendRequestMethod.MakeGenericMethod(returnType).Invoke(this.Connection, new[] { requestAttr.Method, args });
        }

        if (notificationAttr is not null)
        {
            // It's a notification
            var args = arguments.FirstOrDefault();
            this.Connection.PostNotification(notificationAttr.Method, args);
            return Task.CompletedTask;
        }

        return null;
    }
}
