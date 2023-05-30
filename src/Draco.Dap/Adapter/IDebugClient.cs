namespace Draco.Dap.Adapter;

/// <summary>
/// An interface representing the debug client on the remote.
/// </summary>
public interface IDebugClient
{
    /// <summary>
    /// The RPC connection between the client and the server.
    /// </summary>
    public DebugAdapterConnection Connection { get; }
}
