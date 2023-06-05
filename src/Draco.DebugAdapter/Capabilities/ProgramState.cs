using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Draco.Dap.Attributes;
using Draco.Dap.Model;

namespace Draco.DebugAdapter;

internal sealed partial class DracoDebugAdapter
{
    private Debugger.Thread? currentThread;

    [Request("stackTrace")]
    public Task<StackTraceResponse> GetStackTraceAsync(StackTraceArguments args)
    {
        this.translator.ClearCache();
        var thread = this.debugger.Threads.FirstOrDefault(t => t.Id == args.ThreadId);
        var result = thread is null
            ? Array.Empty<StackFrame>()
            : thread.CallStack.Select(this.translator.ToDap).ToArray();
        return Task.FromResult(new StackTraceResponse()
        {
            StackFrames = result,
            TotalFrames = result.Length,
        });
    }

    [Request("scopes")]
    public Task<ScopesResponse> GetScopesAsync(ScopesArguments args)
    {
        var frame = this.translator.GetStackFrameById(args.FrameId);
        if (frame is null)
        {
            return Task.FromResult(new ScopesResponse()
            {
                Scopes = Array.Empty<Scope>(),
            });
        }

        // We build up exactly two scopes, arguments and locals
        var argsId = this.translator.CacheValue(frame.Arguments);
        var argumentsScope = new Scope()
        {
            Expensive = false,
            Name = "Arguments",
            VariablesReference = argsId,
        };
        var localsId = this.translator.CacheValue(frame.Locals);
        var localsScope = new Scope()
        {
            Expensive = false,
            Name = "Locals",
            VariablesReference = localsId,
        };
        return Task.FromResult(new ScopesResponse()
        {
            Scopes = new[] { argumentsScope, localsScope },
        });
    }

    public Task<VariablesResponse> GetVariablesAsync(VariablesArguments args)
    {
        var variables = this.translator.GetVariables(args.VariablesReference);
        return Task.FromResult(new VariablesResponse()
        {
            Variables = variables,
        });
    }
}
