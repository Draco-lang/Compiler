using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Dap.Attributes;
using Draco.Dap.Model;

namespace Draco.DebugAdapter;

internal sealed partial class DracoDebugAdapter
{
    [Request("stackTrace")]
    public Task<StackTraceResponse> GetStackTraceAsync(StackTraceArguments args)
    {
        return Task.FromResult(new StackTraceResponse()
        {
            // TODO: Hardcoded
            StackFrames = new StackFrame[]
            {
                new StackFrame()
                {
                    Id = 0,
                    Column = 0,
                    Line= 3,
                    Name = "main",
                }
            },
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
}
