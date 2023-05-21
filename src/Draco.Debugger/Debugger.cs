using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClrDebug;

namespace Draco.Debugger;

public sealed class Debugger
{
    public Task Terminated => this.terminatedCompletionSource.Task;
    internal Task Started => this.startedCompletionSource.Task;

    private readonly DebuggerHost host;
    private readonly CorDebug corDebug;
    private readonly CorDebugManagedCallback corDebugManagedCallback;
    private readonly CorDebugProcess corDebugProcess;

    private readonly TaskCompletionSource startedCompletionSource = new();
    private readonly TaskCompletionSource terminatedCompletionSource = new();

    internal Debugger(
        DebuggerHost host,
        CorDebug corDebug,
        CorDebugManagedCallback corDebugManagedCallback,
        CorDebugProcess corDebugProcess)
    {
        this.host = host;
        this.corDebug = corDebug;
        this.corDebugManagedCallback = corDebugManagedCallback;
        this.corDebugProcess = corDebugProcess;

        this.InitializeEventHandler();
    }

    private void InitializeEventHandler()
    {
        this.corDebugManagedCallback.OnLoadModule += (sender, args) =>
        {
            Console.WriteLine($"Loaded module {args.Module.Name}");
        };
        this.corDebugManagedCallback.OnCreateProcess += (sender, args) =>
        {
            this.corDebugProcess.Stop(-1);
            this.startedCompletionSource.SetResult();
        };
        this.corDebugManagedCallback.OnExitProcess += (sender, args) =>
        {
            this.terminatedCompletionSource.SetResult();
        };
    }

    public void Resume() => this.corDebugProcess.Continue(false);
}
