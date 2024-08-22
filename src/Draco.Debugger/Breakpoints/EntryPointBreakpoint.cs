using ClrDebug;

namespace Draco.Debugger.Breakpoints;

internal sealed class EntryPointBreakpoint(
    SessionCache sessionCache,
    CorDebugFunctionBreakpoint corDebugBreakpoint) : MethodBreakpoint(sessionCache, corDebugBreakpoint)
{
    public override bool IsEntryPoint => true;
}
