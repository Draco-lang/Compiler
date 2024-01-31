using System.Reflection;

namespace Draco.JsonRpc;

/// <summary>
/// Utilities for construction <see cref="IJsonRpcMethodHandler"/>s.
/// </summary>
internal static class JsonRpcMethodHandler
{
    /// <summary>
    /// Constructs a new <see cref="IJsonRpcMethodHandler"/> from a given <see cref="MethodInfo"/>.
    /// </summary>
    /// <param name="handlerMethod">The handler method info.</param>
    /// <param name="target">The invocation target.</param>
    /// <param name="methodName">The invoked RPC method name.</param>
    /// <param name="isRequest">True, if this is a request handler, false if this is a notification handler.</param>
    /// <param name="isMutating">True, if the method has mutating semantics associated, false otherwise.</param>
    /// <returns>The constructed handler.</returns>
    public static IJsonRpcMethodHandler Create(
        MethodInfo handlerMethod,
        object? target,
        string methodName,
        bool isRequest,
        bool isMutating) => new MethodInfoHandler(handlerMethod, target, methodName, isRequest, isMutating);

    private sealed class MethodInfoHandler : IJsonRpcMethodHandler
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

        public MethodInfoHandler(
            MethodInfo handlerMethod,
            object? target,
            string methodName,
            bool isRequest,
            bool isMutating)
        {
            this.MethodName = methodName;
            this.IsRequest = isRequest;
            this.Mutating = isMutating;

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

        public Task InvokeNotification(object?[] args) =>
            (Task)this.handlerMethod.Invoke(this.target, args.ToArray())!;

        public async Task<object?> InvokeRequest(object?[] args)
        {
            var task = (Task)this.handlerMethod.Invoke(this.target, args.ToArray())!;
            await task;

            if (this.DeclaredReturnType == typeof(Task))
            {
                return null;
            }
            else
            {
                var getResult = (MethodInfo)task.GetType().GetMemberWithSameMetadataDefinitionAs(taskGetResult);
                return getResult.Invoke(task, null);
            }
        }
    }
}
