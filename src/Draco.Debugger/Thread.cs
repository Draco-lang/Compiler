using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    /// The ID of this thread.
    /// </summary>
    public int Id => this.CorDebugThread.Id;

    /// <summary>
    /// The name of this thread.
    /// </summary>
    public string? Name
    {
        get => this.name ??= this.BuildName();
        internal set => this.name = value;
    }
    private string? name;

    private readonly List<Breakpoint> currentlyHitBreakpoints = new();

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

    internal void ClearCache()
    {
        this.callStack = null;
    }

    internal void ClearCurrentlyHitBreakpoints()
    {
        foreach (var breakpoint in this.currentlyHitBreakpoints)
        {
            if (!breakpoint.HitTcs.Task.IsCompleted) throw new InvalidOperationException("Unexpected not-hit breakpoint.");
            breakpoint.HitTcs = new();
        }
        this.currentlyHitBreakpoints.Clear();
    }

    /// <summary>
    /// Steps into the current call.
    /// </summary>
    public void StepInto()
    {
        this.DisableAllSteppers();
        this.ClearCurrentlyHitBreakpoints();
        var stepper = this.BuildCorDebugStepper();
        if (this.TryGetStepRange(out var range))
        {
            stepper.StepRange(true, new[] { range }, 1);
        }
        else
        {
            stepper.Step(true);
        }
        this.CorDebugThread.Process.Continue(false);
    }

    /// <summary>
    /// Steps over the current statement.
    /// </summary>
    public void StepOver()
    {
        this.DisableAllSteppers();
        this.ClearCurrentlyHitBreakpoints();

        var stepper = this.BuildCorDebugStepper();
        if (this.TryGetStepRange(out var range))
        {
            stepper.StepRange(false, new[] { range }, 1);
        }
        else
        {
            stepper.Step(false);
        }
        this.CorDebugThread.Process.Continue(false);
    }

    /// <summary>
    /// Steps out of the current call.
    /// </summary>
    public void StepOut()
    {
        this.DisableAllSteppers();
        this.ClearCurrentlyHitBreakpoints();

        var stepper = this.BuildCorDebugStepper();
        stepper.StepOut();
        this.CorDebugThread.Process.Continue(false);
    }

    private string? BuildName()
    {
        var threadObject = ValueUtils.ToBrowsableObject(this.CorDebugThread.Object);
        if (threadObject is not ObjectValue objectValue) return null;
        if (!objectValue.TryGetValue("_name", out var name)) return null;
        return name as string;
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

    private bool TryGetStepRange(out COR_DEBUG_STEP_RANGE range)
    {
        var frame = this.CorDebugThread.ActiveFrame;
        if (frame is not CorDebugILFrame ilFrame)
        {
            range = default;
            return false;
        }

        var ilOffset = ilFrame.IP.pnOffset;
        var method = this.SessionCache.GetMethod(frame.Function);
        var seqPoints = method.SequencePoints;

        for (var i = 1; i < seqPoints.Length; ++i)
        {
            var p = seqPoints[i];
            if (p.Offset <= ilOffset) continue;
            if (p.IsHidden) continue;

            var startOffs = seqPoints[0].Offset;
            for (var j = i - 1; j > 0; --j)
            {
                if (seqPoints[j].Offset > ilOffset) continue;

                startOffs = seqPoints[j].Offset;
                break;
            }
            var endOffs = p.Offset;
            range.startOffset = startOffs;
            range.endOffset = endOffs;
            return true;
        }

        range = default;
        return false;
    }

    private void DisableAllSteppers()
    {
        var process = this.CorDebugThread.Process;
        foreach (var appDomain in process.AppDomains)
        {
            foreach (var stepper in appDomain.Steppers)
            {
                stepper.Deactivate();
            }
        }
    }

    internal void AddStoppedBreakpoint(Breakpoint breakpoint) => this.currentlyHitBreakpoints.Add(breakpoint);
}
