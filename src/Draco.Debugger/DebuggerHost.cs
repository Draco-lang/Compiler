using System;
using System.Threading;
using System.Threading.Tasks;
using ClrDebug;

namespace Draco.Debugger;

/// <summary>
/// A host object that can be used to start debugger processes.
/// </summary>
public sealed class DebuggerHost
{
    public static DebuggerHost Create(string dbgshimPath)
    {
        var dbgshim = new DbgShim(NativeMethods.LoadLibrary(dbgshimPath));
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
    public async Task<Debugger> StartProcess(string programPath, params string[] args)
    {
        var debugger = null as Debugger;
        var unregisterToken = IntPtr.Zero;
        // TODO: Naive, but temporarily will work
        var command = $"{programPath} {string.Join(' ', args)}";
        var process = this.dbgShim.CreateProcessForLaunch(command, bSuspendProcess: true);

        try
        {
            var wait = new AutoResetEvent(false);
            unregisterToken = this.dbgShim.RegisterForRuntimeStartup(process.ProcessId, (raw, param, hresult) =>
            {
                var corDbg = new CorDebug(raw);
                corDbg.Initialize();

                var cb = new CorDebugManagedCallback();
                corDbg.SetManagedHandler(cb);

                var corDbgProcess = corDbg.DebugActiveProcess(process.ProcessId, win32Attach: false);
                debugger = new(this, programPath, corDbg, cb, corDbgProcess);

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

        await debugger!.Started;
        return debugger!;
    }
}
