using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.JsonRpc;

/// <summary>
/// A message adapter to query info about messages.
/// </summary>
/// <typeparam name="TMessage">The message type this is an adapter for.</typeparam>
public interface IJsonRpcMessageAdapter<TMessage>
{
    /// <summary>
    /// Checks, if <paramref name="message"/> is a request.
    /// </summary>
    /// <param name="message">The message to check.</param>
    /// <returns>True, if <paramref name="message"/> is a request, false otherwise.</returns>
    public static abstract bool IsRequest(TMessage message);

    /// <summary>
    /// Checks, if <paramref name="message"/> is a response.
    /// </summary>
    /// <param name="message">The message to check.</param>
    /// <returns>True, if <paramref name="message"/> is a response, false otherwise.</returns>
    public static abstract bool IsResponse(TMessage message);

    /// <summary>
    /// Checks, if <paramref name="message"/> is a notification.
    /// </summary>
    /// <param name="message">The message to check.</param>
    /// <returns>True, if <paramref name="message"/> is a notification, false otherwise.</returns>
    public static abstract bool IsNotification(TMessage message);

    /// <summary>
    /// Retrieves the ID for <paramref name="message"/>, in case there is one.
    /// </summary>
    /// <param name="message">The message to retrieve the ID for.</param>
    /// <returns>The ID of <paramref name="message"/>, or null if it has none.</returns>
    public static abstract object? GetId(TMessage message);
}
