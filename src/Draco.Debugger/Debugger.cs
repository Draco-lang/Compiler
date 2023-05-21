using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
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

    private CorDebugAssembly? corDebugAssembly;

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
        this.corDebugManagedCallback.OnAnyEvent += (sender, args) =>
        {
            if (args.Kind == CorDebugManagedCallbackKind.Breakpoint) return;
            this.Resume();
        };
        this.corDebugManagedCallback.OnLoadModule += (sender, args) =>
        {
            Console.WriteLine($"Loaded module {args.Module.Name}");
        };
        var assemblyCount = 0;
        this.corDebugManagedCallback.OnLoadAssembly += (sender, args) =>
        {
            // TODO: Is this reliable?
            if (assemblyCount == 1)
            {
                this.corDebugAssembly = args.Assembly;
                this.corDebugProcess.Stop(-1);
                this.startedCompletionSource.SetResult();
            }
            ++assemblyCount;
        };
        this.corDebugManagedCallback.OnExitProcess += (sender, args) =>
        {
            this.terminatedCompletionSource.SetResult();
        };
        this.corDebugManagedCallback.OnBreakpoint += (sender, args) =>
        {
            // TODO
            var x = 0;
        };
    }

    public void Resume() => this.corDebugProcess.TryContinue(false);

    public void SetBreakpoint(int methodDefinitionHandle, int offset)
    {
        Debug.Assert(this.corDebugAssembly is not null);
        var module = this.corDebugAssembly.Modules.Single();
        var function = module.GetFunctionFromToken(new mdMethodDef(methodDefinitionHandle));
        var code = function.ILCode;
        var bp = code.CreateBreakpoint(offset);
    }
}
