namespace Draco.JsonRpc;

/// <summary>
/// The interface all JSON-RPC clients must implement.
/// </summary>
internal interface IJsonRpcClient
{
    /// <summary>
    /// The connection.
    /// </summary>
    public IJsonRpcConnection Connection { get; }
}
