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

    [Event("initialized")]
    public Task Initialized();

    [Event("output", Mutating = true)]
    public Task SendOutputAsync(OutputEvent args);

    [Event("process")]
    public Task ProcessStartedAsync(ProcessEvent args);

    [Event("breakpoint")]
    public Task UpdateBreakpointAsync(BreakpointEvent args);

    [Event("stopped")]
    public Task StoppedAsync(StoppedEvent args);

    [Event("exited")]
    public Task ProcessExitedAsync(ExitedEvent args);

    [Event("terminated")]
    public Task DebuggerTerminatedAsync(TerminatedEvent args);
}
