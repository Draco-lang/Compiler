using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Draco.JsonRpc;
using Draco.Lsp.Attributes;

namespace Draco.Lsp.Server;

internal sealed class LanguageServerMethodHandler : IJsonRpcMethodHandler
{
    private static readonly MethodInfo taskGetResult = typeof(Task<>).GetMethod("get_Result")!;

    public string MethodName { get; }
    public bool IsRequest { get; }
    public bool IsNotification => !this.IsRequest;
    public bool AcceptsParams => this.DeclaredParamsType is not null;
    public bool SupportsCancellation { get; }
    public bool Mutating { get; }
    public Type? DeclaredParamsType { get; }
    public Type DeclaredReturnType { get; }

    private readonly MethodInfo handlerMethod;
    private readonly object? target;

    public LanguageServerMethodHandler(MethodInfo handlerMethod, object? target)
    {
        this.handlerMethod = handlerMethod;
        this.target = target;

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

            this.SupportsCancellation = true;
            this.DeclaredParamsType = parameters[0].ParameterType;
            break;
        }
        case 1:
        {
            var type = parameters[0].ParameterType;
            if (type == typeof(CancellationToken))
            {
                this.SupportsCancellation = true;
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

        this.IsRequest = requestAttr is not null;

        // Verify return type
        var returnType = handlerMethod.ReturnType;
        if (this.IsRequest && returnType != typeof(Task) && returnType.GetGenericTypeDefinition() != typeof(Task<>))
        {
            throw new ArgumentException("Request handler must return Task or Task<T>.", nameof(handlerMethod));
        }

        if (!this.IsRequest && returnType != typeof(Task))
        {
            throw new ArgumentException("Notification handler must return Task.", nameof(handlerMethod));
        }

        this.DeclaredReturnType = returnType;
    }

    public Task InvokeNotification(object?[] args)
    {
        Debug.WriteLine($"InvokeNotification {this.MethodName} START");
        var result = (Task)this.handlerMethod.Invoke(this.target, args.ToArray())!;
        Debug.WriteLine($"InvokeNotification {this.MethodName} END");
        return result;
    }

    public async Task<object?> InvokeRequest(object?[] args)
    {
        Debug.WriteLine($"InvokeRequest {this.MethodName} START");
        var task = (Task)this.handlerMethod.Invoke(this.target, args.ToArray())!;
        Debug.WriteLine($"InvokeRequest {this.MethodName} END");
        await task;

        if (this.DeclaredReturnType == typeof(Task))
        {
            return null;
        }
        else
        {
            Debug.WriteLine($"EEEE {this.MethodName} START");
            var getResult = (MethodInfo)task.GetType().GetMemberWithSameMetadataDefinitionAs(taskGetResult);
            Debug.WriteLine($"EEEE {this.MethodName} END");
            return getResult.Invoke(task, null);
        }
    }
}
