using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;
using ClrDebug;
using Draco.Debugger.IO;

namespace Draco.Debugger;

/// <summary>
/// Represents a debugger for a single process.
/// </summary>
public sealed class Debugger
{
    /// <summary>
    /// The task that is completed, when the process has terminated.
    /// </summary>
    public Task Terminated => this.terminatedCompletionSource.Task;

    /// <summary>
    /// The main user-module.
    /// </summary>
    public Module MainModule => this.mainModule
        ?? throw new InvalidOperationException("the main module has not been loaded yet");

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

    /// <summary>
    /// A writer to the processes standard input.
    /// </summary>
    public StreamWriter StandardInput => this.ioWorker.StandardInput;

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
    /// The event that is triggered on a debugger event logged.
    /// </summary>
    public event EventHandler<string>? OnEventLog;

    /// <summary>
    /// True, if the debugger should stop at the entry-point.
    /// </summary>
    internal bool StopAtEntryPoint { get; init; }

    private readonly CorDebugProcess corDebugProcess;
    private readonly IoWorker<CorDebugProcess> ioWorker;

    private readonly TaskCompletionSource terminatedCompletionSource = new();
    private readonly CancellationTokenSource terminateTokenSource = new();

    private readonly SessionCache sessionCache = new();

    private Breakpoint? entryPointBreakpoint;
    private Module? mainModule;

    internal Debugger(
        CorDebugProcess corDebugProcess,
        IoWorker<CorDebugProcess> ioWorker,
        CorDebugManagedCallback cb)
    {
        this.corDebugProcess = corDebugProcess;
        this.ioWorker = ioWorker;

        this.InitializeEventHandler(cb);
        ioWorker.Run(this.terminateTokenSource.Token);
    }

    /// <summary>
    /// Resumes the execution of the program.
    /// </summary>
    public void Continue() => this.corDebugProcess.TryContinue(false);

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
        this.Continue();
    }

    private void OnCreateThreadHandler(object? sender, CreateThreadCorDebugManagedCallbackEventArgs args)
    {
        this.Continue();
    }

    private void OnExitThreadHandler(object? sender, ExitThreadCorDebugManagedCallbackEventArgs args)
    {
        this.Continue();
    }

    private void OnBreakpointHandler(object? sender, BreakpointCorDebugManagedCallbackEventArgs args)
    {
        var breakpoint = this.sessionCache.GetBreakpoint(args.Breakpoint);
        if (this.entryPointBreakpoint == breakpoint)
        {
            // This was the entry point breakpoint
            this.entryPointBreakpoint.CorDebugBreakpoint.Activate(false);
        }

        this.OnBreakpoint?.Invoke(sender, new()
        {
            Thread = this.sessionCache.GetThread(args.Thread),
            Breakpoint = breakpoint,
        });
    }

    private void OnStepCompleteHandler(object? sender, StepCompleteCorDebugManagedCallbackEventArgs args)
    {
        var frame = args.Thread.ActiveFrame;
        if (frame is not CorDebugILFrame ilFrame) return;

        var offset = ilFrame.IP.pnOffset;
        var function = this.sessionCache.GetMethod(args.Thread.ActiveFrame.Function);
        var range = function.GetSourceRangeForOffset(offset);

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
        this.terminateTokenSource.Cancel();
        this.terminatedCompletionSource.SetResult();
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
        if (this.entryPointBreakpoint is not null || !this.StopAtEntryPoint) return;

        // We do
        var method = corModule.GetFunctionFromToken(MetadataTokens.GetToken(entryPointToken));
        var code = method.ILCode;
        var corDebugBreakpoint = code.CreateBreakpoint(0);
        // Cache it
        this.entryPointBreakpoint = this.sessionCache.GetBreakpoint(corDebugBreakpoint, isEntryPoint: true);
    }
}
