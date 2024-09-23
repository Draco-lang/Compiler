using System;
using System.Diagnostics;
using Draco.Coverage;

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

    /// <summary>
    /// The shared memory with the process.
    /// </summary>
    public SharedMemory<int> SharedMemory
    {
        get => this.sharedMemory ?? throw new InvalidOperationException("the shared memory is not set");
        set => this.sharedMemory = value;
    }
    private SharedMemory<int>? sharedMemory;
}
