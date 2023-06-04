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
        return Task.FromResult(new StackTraceResponse()
        {
            StackFrames = this.BuildCallStack(),
        });
    }

    [Request("scopes")]
    public Task<ScopesResponse> GetScopesAsync(ScopesArguments args)
    {
        return Task.FromResult(new ScopesResponse()
        {
            // TODO: Hardcoded
            Scopes = new Scope[]
            {
            },
        });
    }

    private IList<StackFrame> BuildCallStack() => this.currentThread is null
        ? Array.Empty<StackFrame>()
        : this.currentThread.CallStack.Select(this.TranslateStackFrame).ToList();

    private StackFrame TranslateStackFrame(Debugger.StackFrame frame) => new()
    {
        Id = RuntimeHelpers.GetHashCode(frame),
        Column = 0,
        Line = 0,
        Name = frame.Method.Name,
        Source = this.TranslateSource(frame.Method.SourceFile),
    };
}
