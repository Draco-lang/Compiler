using System.Threading.Tasks;
using Draco.Dap.Attributes;
using Draco.Dap.Model;

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

    [Event("output", Mutating = true)]
    public Task SendOutput(OutputEvent args);

    [Event("exited", Mutating = true)]
    public Task ProcessExited(ExitedEvent args);

    [Event("terminated", Mutating = true)]
    public Task DebuggerTerminated(TerminatedEvent args);
}
