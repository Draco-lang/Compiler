using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Draco.Dap.Attributes;

namespace Draco.Dap.Adapter;

internal sealed class DebugAdapterMethodHandler
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

    internal DebugAdapterMethodHandler(MethodInfo handlerMethod, object? target)
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
        var eventAttr = handlerMethod.GetCustomAttribute<EventAttribute>();

        if (requestAttr is null && eventAttr is null)
        {
            throw new ArgumentException($"Handler must be marked as either a request or event handler.", nameof(handlerMethod));
        }

        if (requestAttr is not null && eventAttr is not null)
        {
            throw new ArgumentException($"Handler can not be marked as both a request and event handler.", nameof(handlerMethod));
        }

        this.MethodName = requestAttr?.Method ?? eventAttr!.Method;
        this.Mutating = requestAttr?.Mutating ?? eventAttr!.Mutating;

        this.ProducesResponse = requestAttr is not null;

        // Verify return type
        var returnType = handlerMethod.ReturnType;
        if (this.ProducesResponse && returnType != typeof(Task) && returnType.GetGenericTypeDefinition() != typeof(Task<>))
        {
            throw new ArgumentException("Request handler must return Task or Task<T>.", nameof(handlerMethod));
        }

        if (!this.ProducesResponse && returnType != typeof(Task))
        {
            throw new ArgumentException("Event handler must return Task.", nameof(handlerMethod));
        }

        this.DeclaredReturnType = returnType;
    }
}
