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
