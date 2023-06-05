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
        var result = this.BuildCallStack();
        return Task.FromResult(new StackTraceResponse()
        {
            StackFrames = result,
            TotalFrames = result.Count,
        });
    }

    [Request("scopes")]
    public Task<ScopesResponse> GetScopesAsync(ScopesArguments args)
    {
        var frame = this.currentThread?.CallStack.FirstOrDefault(c => c.Id == args.FrameId);
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

    private IList<StackFrame> BuildCallStack() => this.currentThread is null
        ? Array.Empty<StackFrame>()
        : this.currentThread.CallStack.Select(this.translator.ToDap).ToList();
}
