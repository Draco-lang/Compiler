using System;

namespace Draco.Debugger;

/// <summary>
/// The event arguments for the event when a breakpoint is hit.
/// </summary>
public sealed class OnBreakpointEventArgs : EventArgs
{
    /// <summary>
    /// The thread that was stopped.
    /// </summary>
    public required Thread Thread { get; init; }

    /// <summary>
    /// The breakpoint we stopped at.
    /// </summary>
    public required Breakpoint Breakpoint { get; init; }
}
