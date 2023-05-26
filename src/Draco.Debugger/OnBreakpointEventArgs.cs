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
    public Thread Thread { get; init; } = null!;

    /// <summary>
    /// The source file where the breakpoint is located.
    /// </summary>
    public SourceFile? SourceFile { get; init; }

    /// <summary>
    /// The range of the breakpoint.
    /// </summary>
    public SourceRange? Range { get; init; }
}
