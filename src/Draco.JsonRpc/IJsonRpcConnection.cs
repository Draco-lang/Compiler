using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.JsonRpc;

/// <summary>
/// Represents a JSON RPC connection.
/// </summary>
public interface IJsonRpcConnection
{
    /// <summary>
    /// Registers a message handler.
    /// </summary>
    /// <param name="handler">The handler to register.</param>
    public void AddHandler(IJsonRpcMethodHandler handler);

    /// <summary>
    /// Starts listening on the connection.
    /// </summary>
    /// <returns>The task that completes when the connection closes.</returns>
    public Task ListenAsync();

    /// <summary>
    /// Shuts down this connection.
    /// </summary>
    public void Shutdown();

    /// <summary>
    /// Sends a request to the client.
    /// </summary>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="method">The method name.</param>
    /// <param name="params">The parameter passed in with the request.</param>
    /// <returns>The task with the response that completes, when the client responds.</returns>
    public Task<TResponse?> SendRequestAsync<TResponse>(string method, object? @params);

    /// <summary>
    /// Sends a request to the client.
    /// </summary>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="method">The method name.</param>
    /// <param name="params">The parameter passed in with the request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task with the response that completes, when the client responds.</returns>
    public Task<TResponse?> SendRequestAsync<TResponse>(string method, object? @params, CancellationToken cancellationToken);

    /// <summary>
    /// Sends a notification to the client.
    /// </summary>
    /// <param name="method">The method name.</param>
    /// <param name="params">The parameters passed in with the notification.</param>
    /// <returns>The task that completes when the notification is sent.</returns>
    public Task SendNotificationAsync(string method, object? @params);
}
