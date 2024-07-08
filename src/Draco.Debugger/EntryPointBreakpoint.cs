using ClrDebug;

namespace Draco.Debugger;

internal sealed class EntryPointBreakpoint(
    SessionCache sessionCache,
    CorDebugFunctionBreakpoint corDebugBreakpoint) : MethodBreakpoint(sessionCache, corDebugBreakpoint)
{
    public override bool IsEntryPoint => true;
}
