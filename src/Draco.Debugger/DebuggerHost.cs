using System;
using System.Runtime.InteropServices;
using System.Threading;
using ClrDebug;
using Draco.Debugger.IO;

namespace Draco.Debugger;

/// <summary>
/// A host object that can be used to start debugger processes.
/// </summary>
public sealed class DebuggerHost
{
    public static DebuggerHost Create()
    {
        var dbgshim = new DbgShim(NativeLibrary.Load("dbgshim", typeof(DebuggerHost).Assembly, null));
        return new(dbgshim);
    }

    private readonly DbgShim dbgShim;

    private DebuggerHost(DbgShim dbgShim)
    {
        this.dbgShim = dbgShim;
    }

    /// <summary>
    /// Starts a new debugging process paused.
    /// </summary>
    /// <param name="programPath">The path to the program to be executed.</param>
    /// <param name="args">The arguments to invoke the program with.</param>
    /// <returns>The debugger instance responsible for debugging the startedf process.</returns>
    public Debugger StartProcess(string programPath, params string[] args)
    {
        var debugger = null as Debugger;
        var unregisterToken = IntPtr.Zero;
        // TODO: Naive, but temporarily will work
        var command = $"{programPath} {string.Join(' ', args)}";
        // Start the process with STDIO captured
        var process = IoUtils.CaptureProcess(
            () => this.dbgShim.CreateProcessForLaunch(command, bSuspendProcess: true),
            out var ioHandles);

        try
        {
            var wait = new AutoResetEvent(false);
            unregisterToken = this.dbgShim.RegisterForRuntimeStartup(process.ProcessId, (pCordb, param, hresult) =>
            {
                pCordb.Initialize();

                var cb = new CorDebugManagedCallback();
                pCordb.SetManagedHandler(cb);
                debugger = new(cb: cb) // We must create the debugger which register event before starting the debugger, or there is are concurrency issues.
                {
                    StopAtEntryPoint = true,
                };
                var corDbgProcess = pCordb.DebugActiveProcess(process.ProcessId, win32Attach: false);

                debugger.Init(corDebugProcess: corDbgProcess, ioWorker: new(corDbgProcess, ioHandles));

                wait.Set();
            });

            this.dbgShim.ResumeProcess(process.ResumeHandle);
            wait.WaitOne();
        }
        finally
        {
            if (unregisterToken != IntPtr.Zero) this.dbgShim.UnregisterForRuntimeStartup(unregisterToken);
            this.dbgShim.CloseResumeHandle(process.ResumeHandle);
        }

        return debugger!;
    }
}
