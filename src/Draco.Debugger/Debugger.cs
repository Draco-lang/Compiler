using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClrDebug;

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
    /// The event that triggers, when a breakpoint is hit.
    /// </summary>
    public event EventHandler<OnBreakpointEventArgs>? OnBreakpoint;

    /// <summary>
    /// True, if the debugger should stop at the entry-point.
    /// </summary>
    internal bool StopAtEntryPoint { get; init; }

    private readonly CorDebugProcess corDebugProcess;

    private readonly TaskCompletionSource terminatedCompletionSource = new();

    private CorDebugBreakpoint? entryPointBreakpoint;

    internal Debugger(
        CorDebugProcess corDebugProcess,
        CorDebugManagedCallback cb)
    {
        this.corDebugProcess = corDebugProcess;

        this.InitializeEventHandler(cb);
    }

    /// <summary>
    /// Resumes the execution of the program.
    /// </summary>
    public void Continue() => this.corDebugProcess.TryContinue(false);

    private void InitializeEventHandler(CorDebugManagedCallback cb)
    {
        cb.OnAnyEvent += (sender, args) =>
        {
            var x = 0;
        };

        cb.OnCreateProcess += this.OnCreateProcessHandler;
        cb.OnCreateAppDomain += this.OnCreateAppDomainHandler;
        cb.OnLoadAssembly += this.OnLoadAssemblyHandler;
        cb.OnLoadModule += this.OnLoadModuleHandler;
        cb.OnNameChange += this.OnNameChangeHandler;
        cb.OnCreateThread += this.OnCreateThreadHandler;
        cb.OnExitThread += this.OnExitThreadHandler;
        cb.OnBreakpoint += this.OnBreakpointHandler;
        cb.OnExitProcess += this.OnExitProcessHandler;
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
        this.SetEntryPointBreakpointIfNeeded(args.Module);
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
        this.OnBreakpoint?.Invoke(sender, new()
        {
            // TODO
            SourceFile = null,
            // TODO
            Range = null,
        });
    }

    private void OnExitProcessHandler(object? sender, ExitProcessCorDebugManagedCallbackEventArgs args)
    {
        this.terminatedCompletionSource.SetResult();
        this.Continue();
    }

    private void SetEntryPointBreakpointIfNeeded(CorDebugModule module)
    {
        if (this.entryPointBreakpoint is not null || !this.StopAtEntryPoint) return;

        using var peReader = new PEReader(File.OpenRead(module.Name));
        var entryPointToken = peReader.GetEntryPoint();
        if (entryPointToken.IsNil) return;

        var method = module.GetFunctionFromToken(MetadataTokens.GetToken(entryPointToken));
        var code = method.ILCode;
        this.entryPointBreakpoint = code.CreateBreakpoint(0);
    }
}
