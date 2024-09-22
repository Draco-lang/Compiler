using System;
using System.Diagnostics;

namespace Draco.Fuzzing;

/// <summary>
/// A reference to a process that can be written by a target executor and can be read by fault detectors.
/// </summary>
public sealed class ProcessReference
{
    /// <summary>
    /// The process being executed.
    /// </summary>
    public Process Process
    {
        get => this.process ?? throw new InvalidOperationException("the process is not set");
        set
        {
            if (this.process is not null && !this.process.HasExited)
            {
                throw new InvalidOperationException("there is a process set that was not waited for");
            }
            this.process = value;
        }
    }
    private Process? process;
}
