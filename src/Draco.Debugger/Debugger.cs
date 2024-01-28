using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;
using ClrDebug;
using Draco.Debugger.IO;

namespace Draco.Debugger;

/// <summary>
/// Represents a debugger for a single process.
/// </summary>
public sealed partial class Debugger
{
    /// <summary>
    /// The task that is completed, when the process has terminated.
    /// </summary>
    public Task Terminated => this.ioWorker.WorkLoopTask;

    /// <summary>
    /// The main user-module.
    /// </summary>
    public Module MainModule => this.mainModule
        ?? throw new InvalidOperationException("the main module has not been loaded yet");

    /// <summary>
    /// The main thread.
    /// </summary>
    public Thread MainThread => this.mainThread
        ?? throw new InvalidOperationException("the main thread has not been started yet");

    /// <summary>
    /// The threads in the debugged process.
    /// </summary>
    public ImmutableArray<Thread> Threads => this.threads.ToImmutable();
    private readonly ImmutableArray<Thread>.Builder threads = ImmutableArray.CreateBuilder<Thread>();

    /// <summary>
    /// This represent the fact that the debugger is ready to be used.
    /// </summary>
    public Task Ready => this.readyTCS.Task;

    /// <summary>
    /// A writer to the processes standard input.
    /// </summary>
    public StreamWriter StandardInput => this.ioWorker.StandardInput;


    /// <summary>
    /// True, if the debugger should stop at the entry-point.
    /// </summary>
    internal bool StopAtEntryPoint { get; init; }

    private readonly CorDebugProcess corDebugProcess;
    private readonly IoWorker<CorDebugProcess> ioWorker;

    private readonly TaskCompletionSource readyTCS = new();

    private readonly SessionCache sessionCache = new();

    private Breakpoint? entryPointBreakpoint;
    private Module? mainModule;
    private Thread? mainThread;

    internal Debugger(
        CorDebugProcess corDebugProcess,
        IoWorker<CorDebugProcess> ioWorker,
        CorDebugManagedCallback cb)
    {
        this.corDebugProcess = corDebugProcess;
        this.ioWorker = ioWorker;

        this.InitializeEventHandler(cb);
        ioWorker.Start();
    }

    private void ClearCache()
    {
        foreach (var t in this.Threads) t.ClearCache();
    }

    /// <summary>
    /// Pauses the execution of the program.
    /// </summary>
    public void Pause()
    {
        if (this.corDebugProcess.TryStop(0) == HRESULT.S_OK)
        {
            this.ClearCache();
            this.OnPause?.Invoke(this.corDebugProcess, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Resumes the execution of the program.
    /// </summary>
    public void Continue()
    {
        foreach (var thread in this.Threads)
        {
            thread.ClearCurrentlyHitBreakpoints();
        }
        this.corDebugProcess.TryContinue(false);
    }
}
