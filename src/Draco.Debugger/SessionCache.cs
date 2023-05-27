using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using ClrDebug;

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
}
