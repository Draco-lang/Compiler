using System;
using System.Reflection.Metadata.Ecma335;
using ClrDebug;

namespace Draco.Debugger;

public partial class Debugger
{
    private void InitializeEventHandler(CorDebugManagedCallback cb)
    {
        cb.OnAnyEvent += this.OnAnyEventHandler;
        cb.OnCreateProcess += this.OnCreateProcessHandler;
        cb.OnCreateAppDomain += this.OnCreateAppDomainHandler;
        cb.OnLoadAssembly += this.OnLoadAssemblyHandler;
        cb.OnLoadModule += this.OnLoadModuleHandler;
        cb.OnNameChange += this.OnNameChangeHandler;
        cb.OnCreateThread += this.OnCreateThreadHandler;
        cb.OnExitThread += this.OnExitThreadHandler;
        cb.OnUnloadModule += this.OnUnloadModuleHandler;
        cb.OnBreakpoint += this.OnBreakpointHandler;
        cb.OnStepComplete += this.OnStepCompleteHandler;
        cb.OnExitProcess += this.OnExitProcessHandler;
    }

    private void OnAnyEventHandler(object? sender, CorDebugManagedCallbackEventArgs args)
    {
        switch (args.Kind)
        {
        case CorDebugManagedCallbackKind.CreateProcess:
        {
            var a = (CreateProcessCorDebugManagedCallbackEventArgs)args;
            this.OnEventLog?.Invoke(this, $"process {a.Process.Id} created");
            break;
        }
        case CorDebugManagedCallbackKind.CreateAppDomain:
        {
            var a = (CreateAppDomainCorDebugManagedCallbackEventArgs)args;
            this.OnEventLog?.Invoke(this, $"app domain {a.AppDomain.Id} created");
            break;
        }
        case CorDebugManagedCallbackKind.LoadAssembly:
        {
            var a = (LoadAssemblyCorDebugManagedCallbackEventArgs)args;
            this.OnEventLog?.Invoke(this, $"assembly {a.Assembly.Name} loaded");
            break;
        }
        case CorDebugManagedCallbackKind.LoadModule:
        {
            var a = (LoadModuleCorDebugManagedCallbackEventArgs)args;
            this.OnEventLog?.Invoke(this, $"module {a.Module.Name} loaded");
            break;
        }
        case CorDebugManagedCallbackKind.NameChange:
        {
            var a = (NameChangeCorDebugManagedCallbackEventArgs)args;
            this.OnEventLog?.Invoke(this, "name change");
            break;
        }
        case CorDebugManagedCallbackKind.CreateThread:
        {
            var a = (CreateThreadCorDebugManagedCallbackEventArgs)args;
            this.OnEventLog?.Invoke(this, $"thread {a.Thread.Id} created");
            break;
        }
        case CorDebugManagedCallbackKind.Breakpoint:
        {
            var a = (BreakpointCorDebugManagedCallbackEventArgs)args;
            this.OnEventLog?.Invoke(this, "breakpoint hit");
            break;
        }
        case CorDebugManagedCallbackKind.ExitThread:
        {
            var a = (ExitThreadCorDebugManagedCallbackEventArgs)args;
            this.OnEventLog?.Invoke(this, $"thread {a.Thread.Id} exited");
            break;
        }
        case CorDebugManagedCallbackKind.ExitProcess:
        {
            var a = (ExitProcessCorDebugManagedCallbackEventArgs)args;
            this.OnEventLog?.Invoke(this, $"process {a.Process.Id} exited");
            break;
        }
        case CorDebugManagedCallbackKind.StepComplete:
        {
            var a = (StepCompleteCorDebugManagedCallbackEventArgs)args;
            this.OnEventLog?.Invoke(this, "step complete");
            break;
        }
        default:
            throw new ArgumentOutOfRangeException(nameof(args));
        }
    }

    private void OnCreateProcessHandler(object? sender, CreateProcessCorDebugManagedCallbackEventArgs args)
    {
        foreach (var breakpoint in this.sessionCache.Breakpoints)
        {
            if (!breakpoint.HitTcs.Task.IsCanceled && !breakpoint.HitTcs.Task.IsCompleted) throw new InvalidOperationException("Breakpoint Task not clean when starting.");

        }
        this.Continue();
    }

    private void OnCreateAppDomainHandler(object? sender, CreateAppDomainCorDebugManagedCallbackEventArgs args)
    {
        this.Continue();
    }

    private void OnLoadAssemblyHandler(object? sender, LoadAssemblyCorDebugManagedCallbackEventArgs args)
    {
        this.Continue();
    }

    private void OnLoadModuleHandler(object? sender, LoadModuleCorDebugManagedCallbackEventArgs args)
    {
        var module = this.sessionCache.GetModule(args.Module);
        this.HandleEntrypoint(module);
        this.Continue();
    }

    private void OnNameChangeHandler(object? sender, NameChangeCorDebugManagedCallbackEventArgs args)
    {
        if (args.Thread is not null)
        {
            // Clear cached name
            var thread = this.sessionCache.GetThread(args.Thread);
            thread.Name = null;
        }

        this.Continue();
    }

    private void OnCreateThreadHandler(object? sender, CreateThreadCorDebugManagedCallbackEventArgs args)
    {
        var thread = this.sessionCache.GetThread(args.Thread);
        this.threads.Add(thread);

        // Set main thread, which is guaranteed to be the first created one
        if (this.mainThread is null)
        {
            this.mainThread = thread;
            this.mainThread.Name = "Main Thread";
        }

        this.Continue();
    }

    private void OnExitThreadHandler(object? sender, ExitThreadCorDebugManagedCallbackEventArgs args)
    {
        var thread = this.sessionCache.GetThread(args.Thread);
        this.threads.Remove(thread);

        this.Continue();
    }

    private void OnBreakpointHandler(object? sender, BreakpointCorDebugManagedCallbackEventArgs args)
    {
        this.ClearCache();
        var breakpoint = this.sessionCache.GetBreakpoint(args.Breakpoint);
        if (this.entryPointBreakpoint == breakpoint)
        {
            // This was the entry point breakpoint, remove it
            this.entryPointBreakpoint.Remove();
            this.readyTCS.SetResult();
        }
        var thread = this.sessionCache.GetThread(args.Thread);
        this.OnBreakpoint?.Invoke(sender, new()
        {
            Thread = thread,
            Breakpoint = breakpoint,
        });
        thread.AddStoppedBreakpoint(breakpoint);
        breakpoint.HitTcs.TrySetResult();
    }

    private void OnStepCompleteHandler(object? sender, StepCompleteCorDebugManagedCallbackEventArgs args)
    {
        this.ClearCache();

        var frame = args.Thread.ActiveFrame;
        if (frame is not CorDebugILFrame ilFrame) return;

        var offset = ilFrame.IP.pnOffset;
        var function = this.sessionCache.GetMethod(args.Thread.ActiveFrame.Function);
        var range = function.GetSourceRangeForIlOffset(offset);

        this.OnStep?.Invoke(sender, new()
        {
            Thread = this.sessionCache.GetThread(args.Thread),
            Method = function,
            Range = range,
        });
    }

    private void OnUnloadModuleHandler(object? sender, UnloadModuleCorDebugManagedCallbackEventArgs args)
    {
        this.Continue();
    }

    private void OnExitProcessHandler(object? sender, ExitProcessCorDebugManagedCallbackEventArgs args)
    {
        foreach (var thread in this.sessionCache.Breakpoints)
        {
            thread.HitTcs.TrySetCanceled();
        }
        // TODO: Get exit code properly
        this.OnExited?.Invoke(sender, 0);
        this.Continue();
    }

    private void HandleEntrypoint(Module module)
    {
        // We determine if this is the entry point by looking up the entry point token
        var entryPointToken = module.PeReader.GetEntryPoint();
        if (entryPointToken.IsNil) return;

        // It is, save it
        this.mainModule = module;

        // Set it up
        var corModule = module.CorDebugModule;
        corModule.JITCompilerFlags = CorDebugJITCompilerFlags.CORDEBUG_JIT_DISABLE_OPTIMIZATION;
        corModule.SetJMCStatus(true, 0, null);

        // Check, if we need to set up a breakpoint
        if (this.entryPointBreakpoint is null && this.StopAtEntryPoint)
        {
            // We do
            var method = corModule.GetFunctionFromToken(MetadataTokens.GetToken(entryPointToken));
            var code = method.ILCode;
            var corDebugBreakpoint = code.CreateBreakpoint(0);
            // Cache it
            this.entryPointBreakpoint = this.sessionCache.GetBreakpoint(corDebugBreakpoint, isEntryPoint: true);
        }

        // This is a speculation, but I think that when the module is loaded, the debugger is 'ready'.
        if (!this.StopAtEntryPoint)
        {
            this.readyTCS.SetResult();
        }
    }
}
