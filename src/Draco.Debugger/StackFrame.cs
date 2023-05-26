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
    /// The internal frame.
    /// </summary>
    internal CorDebugFrame CorDebugFrame { get; }

    /// <summary>
    /// Our wrapper around the frame's method.
    /// </summary>
    internal Method Method { get; }

    /// <summary>
    /// The name of the called method.
    /// </summary>
    public string MethodName => this.Method.Name;

    internal StackFrame(CorDebugFrame corDebugFrame)
    {
        this.CorDebugFrame = corDebugFrame;
        this.Method = new(corDebugFrame.Function);
    }
}
