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
    private readonly Dictionary<CorDebugModule, Module> modules = new();
    private readonly Dictionary<CorDebugThread, Thread> threads = new();
    private readonly Dictionary<CorDebugFunction, Method> methods = new();

    public Module GetModule(CorDebugModule corDebugModule)
    {
        if (!this.modules.TryGetValue(corDebugModule, out var cached))
        {
            cached = new(this, corDebugModule);
            this.modules.Add(corDebugModule, cached);
        }
        return cached;
    }

    public Thread GetThread(CorDebugThread corDebugThread)
    {
        if (!this.threads.TryGetValue(corDebugThread, out var cached))
        {
            cached = new(this, corDebugThread);
            this.threads.Add(corDebugThread, cached);
        }
        return cached;
    }

    public Method GetMethod(CorDebugFunction corDebugFunction)
    {
        if (!this.methods.TryGetValue(corDebugFunction, out var cached))
        {
            cached = new(this, corDebugFunction);
            this.methods.Add(corDebugFunction, cached);
        }
        return cached;
    }
}
