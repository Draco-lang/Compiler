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
    /// The method this breakpoint happened in.
    /// </summary>
    public Method? Method { get; init; }

    /// <summary>
    /// The range of the breakpoint.
    /// </summary>
    public SourceRange? Range { get; init; }

    /// <summary>
    /// The source file where the breakpoint is located.
    /// </summary>
    public SourceFile? SourceFile => this.Method?.SourceFile;
}
