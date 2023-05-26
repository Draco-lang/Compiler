using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClrDebug;

namespace Draco.Debugger;

/// <summary>
/// Represents a running thread.
/// </summary>
public sealed class Thread
{
    /// <summary>
    /// The cache for this object.
    /// </summary>
    internal SessionCache SessionCache { get; }

    /// <summary>
    /// The internal thread representation.
    /// </summary>
    internal CorDebugThread CorDebugThread { get; }

    /// <summary>
    /// The current state of the call-stack.
    /// </summary>
    public ImmutableArray<StackFrame> CallStack => this.callStack ??= this.BuildCallStack();
    private ImmutableArray<StackFrame>? callStack;

    internal Thread(SessionCache sessionCache, CorDebugThread corDebugThread)
    {
        this.SessionCache = sessionCache;
        this.CorDebugThread = corDebugThread;
    }

    /// <summary>
    /// Steps into the current call.
    /// </summary>
    public void StepInto()
    {
        var stepper = this.BuildCorDebugStepper();
        stepper.Step(true);
        this.CorDebugThread.Process.Continue(false);
    }

    /// <summary>
    /// Steps over the current statement.
    /// </summary>
    public void StepOver()
    {
        var stepper = this.BuildCorDebugStepper();
        stepper.Step(false);
        this.CorDebugThread.Process.Continue(false);
    }

    /// <summary>
    /// Steps out of the current call.
    /// </summary>
    public void StepOut()
    {
        var stepper = this.BuildCorDebugStepper();
        stepper.StepOut();
        this.CorDebugThread.Process.Continue(false);
    }

    private ImmutableArray<StackFrame> BuildCallStack()
    {
        var stackWalk = this.CorDebugThread.CreateStackWalk();
        var result = ImmutableArray.CreateBuilder<StackFrame>();

        while (true)
        {
            var status = stackWalk.TryGetFrame(out var corDebugFrame);
            // Native method
            if (status == HRESULT.S_FALSE) goto next_frame;

            // For some reason we can get null frames that need to be skipped
            if (corDebugFrame is null) goto next_frame;

            // Create the frame
            var frame = new StackFrame(this.SessionCache, corDebugFrame);
            result.Add(frame);

        // Try to move to the next frame
        next_frame:
            status = stackWalk.TryNext();
            if (status == HRESULT.CORDBG_S_AT_END_OF_STACK) break;
            if (status != HRESULT.S_OK) throw new InvalidOperationException("failed to continue stack walk");
        }

        return result.ToImmutable();
    }

    private CorDebugStepper BuildCorDebugStepper()
    {
        var stepper = this.CorDebugThread.CreateStepper();

        var interceptMask = CorDebugIntercept.INTERCEPT_ALL & ~(CorDebugIntercept.INTERCEPT_SECURITY | CorDebugIntercept.INTERCEPT_CLASS_INIT);
        stepper.SetInterceptMask(interceptMask);

        var stopMask = CorDebugUnmappedStop.STOP_NONE;
        stepper.SetUnmappedStopMask(stopMask);

        stepper.SetJMC(true);
        return stepper;
    }
}
