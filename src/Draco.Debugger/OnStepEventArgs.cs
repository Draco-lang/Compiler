using System;

namespace Draco.Debugger;

/// <summary>
/// The event arguments for the event when a step is complete.
/// </summary>
public sealed class OnStepEventArgs : EventArgs
{
    /// <summary>
    /// The thread that was stopped.
    /// </summary>
    public Thread Thread { get; init; } = null!;

    /// <summary>
    /// The method this step happened in.
    /// </summary>
    public Method? Method { get; init; }

    /// <summary>
    /// The range of the stepped statement.
    /// </summary>
    public SourceRange? Range { get; init; }

    /// <summary>
    /// The source file where the step landed.
    /// </summary>
    public SourceFile? SourceFile => this.Method?.SourceFile;
}
