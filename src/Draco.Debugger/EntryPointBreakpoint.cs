using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClrDebug;

namespace Draco.Debugger;

internal sealed class EntryPointBreakpoint : MethodBreakpoint
{
    public override bool IsEntryPoint => true;

    public EntryPointBreakpoint(SessionCache sessionCache, CorDebugFunctionBreakpoint corDebugBreakpoint)
        : base(sessionCache, corDebugBreakpoint)
    {
    }
}
