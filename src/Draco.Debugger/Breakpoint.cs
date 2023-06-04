using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClrDebug;

namespace Draco.Debugger;

/// <summary>
/// Represents a breakpoint.
/// </summary>
public abstract class Breakpoint
{
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

    private protected Breakpoint(SessionCache sessionCache)
    {
        this.SessionCache = sessionCache;
    }
}
