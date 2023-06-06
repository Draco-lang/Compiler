using System;
using System.Collections.Generic;
using ClrDebug;
using Draco.Debugger.Platform;

namespace Draco.Debugger;

/// <summary>
/// Caches instantiated modules, threads, ...
/// Essentially everything that is wrapped from the CorDebug API.
/// </summary>
internal sealed class SessionCache
{
    private readonly Dictionary<ICorDebugModule, Module> modules = new();
    private readonly Dictionary<ICorDebugThread, Thread> threads = new();
    private readonly Dictionary<ICorDebugFunction, Method> methods = new();
    private readonly Dictionary<ICorDebugBreakpoint, Breakpoint> breakpoints = new();

    public Module GetModule(CorDebugModule corDebugModule)
    {
        if (!this.modules.TryGetValue(corDebugModule.Raw, out var cached))
        {
            cached = new(this, corDebugModule);
            this.modules.Add(corDebugModule.Raw, cached);
        }
        return cached;
    }

    public Thread GetThread(CorDebugThread corDebugThread)
    {
        if (!this.threads.TryGetValue(corDebugThread.Raw, out var cached))
        {
            cached = new(this, corDebugThread);
            this.threads.Add(corDebugThread.Raw, cached);
        }
        return cached;
    }

    public Method GetMethod(CorDebugFunction corDebugFunction)
    {
        if (!this.methods.TryGetValue(corDebugFunction.Raw, out var cached))
        {
            cached = new(this, corDebugFunction);
            this.methods.Add(corDebugFunction.Raw, cached);
        }
        return cached;
    }

    public Breakpoint GetBreakpoint(CorDebugBreakpoint breakpoint, bool isEntryPoint = false)
    {
        if (!this.breakpoints.TryGetValue(breakpoint.Raw, out var cached))
        {
            cached = this.BuildBreakpoint(breakpoint, isEntryPoint);
            this.breakpoints.Add(breakpoint.Raw, cached);
        }
        return cached;
    }

    public bool RemoveBreakpoint(CorDebugBreakpoint breakpoint) => this.breakpoints.Remove(breakpoint.Raw);

    private Breakpoint BuildBreakpoint(CorDebugBreakpoint breakpoint, bool isEntryPoint) => breakpoint switch
    {
        CorDebugFunctionBreakpoint f when isEntryPoint => new EntryPointBreakpoint(this, f),
        CorDebugFunctionBreakpoint f => new MethodBreakpoint(this, f),
        _ => throw new ArgumentOutOfRangeException(nameof(breakpoint)),
    };
}
