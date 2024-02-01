using System.Threading.Tasks;
using ClrDebug;
using Microsoft.VisualBasic;

namespace Draco.Debugger;

/// <summary>
/// Represents a breakpoint.
/// </summary>
public abstract class Breakpoint
{
    /// <summary>
    /// The <see cref="TaskCompletionSource"/> that controls the <see cref="Hit"/> <see cref="Task"/>.
    /// </summary>
    internal TaskCompletionSource HitTcs = new();

    /// <summary>
    /// The cache for this object.
    /// </summary>
    internal SessionCache SessionCache { get; }

    /// <summary>
    /// The internally wrapped breakpoint.
    /// </summary>
    internal abstract CorDebugBreakpoint CorDebugBreakpoint { get; }

    /// <summary>
    /// True, if this is the entry-point breakpoint.
    /// </summary>
    public virtual bool IsEntryPoint => false;

    /// <summary>
    /// The method the breakpoint belongs to.
    /// </summary>
    public virtual Method? Method => null;

    /// <summary>
    /// The source file the breakpoint is in.
    /// </summary>
    public SourceFile? SourceFile => this.Method?.SourceFile;

    /// <summary>
    /// The source range of this breakpoint.
    /// </summary>
    public virtual SourceRange? Range => null;

    /// <summary>
    /// A task that resolves when this breakpoint is hit.
    /// </summary>
    public Task Hit => this.HitTcs.Task;

    private protected Breakpoint(SessionCache sessionCache)
    {
        this.SessionCache = sessionCache;
    }

    /// <summary>
    /// Removes this breakpoint permanently.
    /// </summary>
    public virtual void Remove()
    {
        this.CorDebugBreakpoint.Activate(false);
        this.SessionCache.RemoveBreakpoint(this.CorDebugBreakpoint);
    }
}
