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
        var frame = this.debugger.Threads
            .SelectMany(t => t.CallStack)
            .FirstOrDefault(c => c.Id == args.FrameId);
        if (frame is null)
        {
            return Task.FromResult(new ScopesResponse()
            {
                Scopes = Array.Empty<Scope>(),
            });
        }

        // We build up exactly two scopes, arguments and locals
        var argumentsScope = new Scope()
        {
            Expensive = false,
            Name = "Arguments",
            VariablesReference = frame.Id,
        };
        var localsScope = new Scope()
        {
            Expensive = false,
            Name = "Locals",
            VariablesReference = int.MaxValue - frame.Id,
        };
        return Task.FromResult(new ScopesResponse()
        {
            Scopes = new[] { argumentsScope, localsScope },
        });
    }

    public Task<VariablesResponse> GetVariablesAsync(VariablesArguments args)
    {
        var frame = this.currentThread?.CallStack
            .FirstOrDefault(f => f.Id == args.VariablesReference || int.MaxValue - f.Id == args.VariablesReference);
        if (frame is null)
        {
            return Task.FromResult(new VariablesResponse()
            {
                Variables = Array.Empty<Variable>(),
            });
        }

        IList<Variable> variables;
        if (frame.Id == args.VariablesReference)
        {
            // Arguments
            variables = frame.Arguments
                .Select(kv => this.translator.ToDap(kv.Key, kv.Value))
                .ToList();
        }
        else
        {
            // Locals
            variables = frame.Locals
                .Select(kv => this.translator.ToDap(kv.Key, kv.Value))
                .ToList();
        }

        return Task.FromResult(new VariablesResponse()
        {
            Variables = variables,
        });
    }
}
