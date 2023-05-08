using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Draco.Lsp.Attributes;

namespace Draco.Lsp.Server;

internal sealed class LanguageServerMethodHandler
{
    internal MethodInfo HandlerMethod { get; }

    internal object? Target { get; }

    internal string MethodName { get; }

    internal bool ProducesResponse { get; }

    internal bool HasCancellation { get; }

    [MemberNotNullWhen(true, nameof(DeclaredParamsType))]
    internal bool HasParams => this.DeclaredParamsType is not null;

    internal bool Mutating { get; }

    internal Type? DeclaredParamsType { get; }

    internal Type DeclaredReturnType { get; }

    internal LanguageServerMethodHandler(MethodInfo handlerMethod, object? target)
    {
        this.HandlerMethod = handlerMethod;
        this.Target = target;

        // Verify parameters
        var parameters = handlerMethod.GetParameters();
        switch (parameters.Length)
        {
        case 2:
        {
            if (parameters[^1].ParameterType != typeof(CancellationToken))
            {
                throw new ArgumentException("The second parameter of a handler must be CancellationToken if it is defined.", nameof(handlerMethod));
            }

            this.HasCancellation = true;
            this.DeclaredParamsType = parameters[0].ParameterType;
            break;
        }
        case 1:
        {
            var type = parameters[0].ParameterType;
            if (type == typeof(CancellationToken))
            {
                this.HasCancellation = true;
            }
            else
            {
                this.DeclaredParamsType = type;
            }

            break;
        }
        case 0:
        {
            // No cancellation, no parameters
            break;
        }
        default:
            throw new ArgumentException("Handler has too many arguments.", nameof(handlerMethod));
        }

        // Verify attributes
        var requestAttr = handlerMethod.GetCustomAttribute<RequestAttribute>();
        var notificationAttr = handlerMethod.GetCustomAttribute<NotificationAttribute>();

        if (requestAttr is null && notificationAttr is null)
        {
            throw new ArgumentException($"Handler must be marked as either a request or notification handler.", nameof(handlerMethod));
        }

        if (requestAttr is not null && notificationAttr is not null)
        {
            throw new ArgumentException($"Handler can not be marked as both a request and notification handler.", nameof(handlerMethod));
        }

        this.MethodName = requestAttr?.Method ?? notificationAttr!.Method;
        this.Mutating = requestAttr?.Mutating ?? notificationAttr!.Mutating;

        this.ProducesResponse = requestAttr is not null;

        // Verify return type
        var returnType = handlerMethod.ReturnType;
        if (this.ProducesResponse && returnType != typeof(Task) && returnType.GetGenericTypeDefinition() != typeof(Task<>))
        {
            throw new ArgumentException("Request handler must return Task or Task<T>.", nameof(handlerMethod));
        }

        if (!this.ProducesResponse && returnType != typeof(Task))
        {
            throw new ArgumentException("Notification handler must return Task.", nameof(handlerMethod));
        }

        this.DeclaredReturnType = returnType;
    }
}
