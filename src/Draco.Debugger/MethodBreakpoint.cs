using ClrDebug;

namespace Draco.Debugger;

// NOTE: Not sealed, entry-point breakpoint reuses this
internal class MethodBreakpoint : Breakpoint
{
    internal override CorDebugFunctionBreakpoint CorDebugBreakpoint { get; }
    public override Method Method => this.SessionCache.GetMethod(this.CorDebugBreakpoint.Function);
    public override SourceRange? Range => this.Method.GetSourceRangeForIlOffset(this.CorDebugBreakpoint.Offset);

    public MethodBreakpoint(SessionCache sessionCache, CorDebugFunctionBreakpoint corDebugBreakpoint)
        : base(sessionCache)
    {
        this.CorDebugBreakpoint = corDebugBreakpoint;
        this.Method.MutableBreakpoints.Add(this);
    }

    public override void Remove()
    {
        this.Method.MutableBreakpoints.Remove(this);
        base.Remove();
    }
}
