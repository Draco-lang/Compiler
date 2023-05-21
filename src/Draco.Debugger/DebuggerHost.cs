using System.Diagnostics;
using System.Security.Cryptography;
using ClrDebug;

namespace Draco.Debugger;

public sealed class DebuggerHost
{
    public static DebuggerHost Create(INativeMethods nativeMethods, string dbgshimPath)
    {
        var dbgshim = new DbgShim(nativeMethods.LoadLibrary(dbgshimPath));
        return new(nativeMethods, dbgshim);
    }

    private readonly INativeMethods nativeMethods;
    private readonly DbgShim dbgShim;

    private DebuggerHost(INativeMethods nativeMethods, DbgShim dbgShim)
    {
        this.nativeMethods = nativeMethods;
        this.dbgShim = dbgShim;
    }

    public async Task<Debugger> StartProcess(string command)
    {
        var debugger = null as Debugger;
        var unregisterToken = IntPtr.Zero;
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
                debugger = new(this, corDbg, cb, corDbgProcess);

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
