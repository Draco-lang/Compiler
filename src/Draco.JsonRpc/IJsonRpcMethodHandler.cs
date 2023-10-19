using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.JsonRpc;

/// <summary>
/// A registered JSON RPC method handler.
/// </summary>
public interface IJsonRpcMethodHandler
{
    /// <summary>
    /// The name of the method this handles.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// True, if this is a request handler.
    /// </summary>
    public bool IsRequest { get; }

    /// <summary>
    /// True, if this is a notification handler.
    /// </summary>
    public bool IsNotification { get; }

    /// <summary>
    /// True, if the handler accepts a parameter object.
    /// </summary>
    public bool AcceptsParams { get; }

    /// <summary>
    /// True, if the method supports cancellation.
    /// </summary>
    public bool SupportsCancellation { get; }

    /// <summary>
    /// True, if this method has mutation semantics associated to it.
    /// </summary>
    public bool Mutating { get; }

    /// <summary>
    /// The declared parameter type.
    /// </summary>
    public Type DeclaredParamsType { get; }

    /// <summary>
    /// The declared return type.
    /// </summary>
    public Type DeclaredReturnType { get; }

    /// <summary>
    /// Invokes this handler as a notification handler.
    /// </summary>
    /// <param name="args">The arguments to call the handler with.</param>
    /// <returns>The task of the invocation.</returns>
    public Task InvokeNotification(object?[] args);

    /// <summary>
    /// Invokes this handler as a request handler.
    /// </summary>
    /// <param name="args">The arguments to call the handler with.</param>
    /// <returns>The task of the invocation.</returns>
    public Task<object?> InvokeRequest(object?[] args);
}
