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

    // TODO: Doc
    public Task<TResponse?> SendRequestAsync<TResponse>(string method, object? @params);

    // TODO: Doc
    public Task<TResponse?> SendRequestAsync<TResponse>(string method, object? @params, CancellationToken cancellationToken);

    // TODO: Doc
    public Task SendNotificationAsync(string method, object? @params);
}
