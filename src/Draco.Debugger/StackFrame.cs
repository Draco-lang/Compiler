using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClrDebug;

namespace Draco.Debugger;

/// <summary>
/// Represents a single frame in the call-stack.
/// </summary>
public sealed class StackFrame
{
    /// <summary>
    /// The cache for this object.
    /// </summary>
    internal SessionCache SessionCache { get; }

    /// <summary>
    /// The internal frame.
    /// </summary>
    internal CorDebugFrame CorDebugFrame { get; }

    /// <summary>
    /// The method the frame represents.
    /// </summary>
    public Method Method => this.SessionCache.GetMethod(this.CorDebugFrame.Function);

    internal StackFrame(SessionCache sessionCache, CorDebugFrame corDebugFrame)
    {
        this.SessionCache = sessionCache;
        this.CorDebugFrame = corDebugFrame;
    }
}
