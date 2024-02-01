using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClrDebug;

namespace Draco.Debugger;

public partial class Debugger
{
    /// <summary>
    /// The debuggee has exited.
    /// </summary>
    public event EventHandler<int>? OnExited;

    /// <summary>
    /// The event that triggers, when a breakpoint is hit.
    /// </summary>
    public event EventHandler<OnBreakpointEventArgs>? OnBreakpoint;

    /// <summary>
    /// The event that triggers, when a step is complete.
    /// </summary>
    public event EventHandler<OnStepEventArgs>? OnStep;

    /// <summary>
    /// The event that triggers, when the program is paused.
    /// </summary>
    public event EventHandler? OnPause;

    /// <summary>
    /// The event that is triggered on a debugger event logged.
    /// </summary>
    public event EventHandler<string>? OnEventLog;

    /// <summary>
    /// The event that triggers when the process writes to its STDOUT.
    /// </summary>
    public event EventHandler<string> OnStandardOut
    {
        add => this.ioWorker.OnStandardOut += value;
        remove => this.ioWorker.OnStandardOut -= value;
    }

    /// <summary>
    /// The event that triggers when the process writes to its STDERR.
    /// </summary>
    public event EventHandler<string> OnStandardError
    {
        add => this.ioWorker.OnStandardError += value;
        remove => this.ioWorker.OnStandardError -= value;
    }
}
