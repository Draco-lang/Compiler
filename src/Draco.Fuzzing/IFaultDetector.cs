using System;
using System.Collections.Generic;
using System.Threading;

namespace Draco.Fuzzing;

/// <summary>
/// Detects a fault or crash in the target.
/// </summary>
public interface IFaultDetector
{
    /// <summary>
    /// Attempts to execute the given action and returns the fault result.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>The fault result.</returns>
    public FaultResult Execute(Action action);
}

/// <summary>
/// Factory for common fault detection logic.
/// </summary>
public static class FaultDetector
{
    /// <summary>
    /// Creates a fault detector from the given function.
    /// </summary>
    /// <param name="func">The fault detection function.</param>
    /// <returns>The fault detector.</returns>
    public static IFaultDetector Create(Func<Action, FaultResult> func) => new DelegateFaultDetector(func);

    /// <summary>
    /// Creates a default fault detector that catches exceptions and timeouts in-process.
    /// </summary>
    /// <param name="timeout">The timeout for the action.</param>
    /// <returns>The fault detector.</returns>
    public static IFaultDetector DefaultInProcess(TimeSpan? timeout = null) => Create(action =>
    {
        timeout ??= TimeSpan.MaxValue;
        var exception = null as Exception;
        var evt = new ManualResetEvent(false);
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            evt.Set();
        });
        if (!evt.WaitOne(timeout.Value)) return FaultResult.Timeout(timeout.Value);
        if (exception is not null) return FaultResult.Exception(exception);
        return FaultResult.Ok;
    });

    /// <summary>
    /// Creates a default fault detector that detects nonzero exit codes and timeouts of a process.
    /// </summary>
    /// <param name="processReference">The process reference.</param>
    /// <param name="timeout">The timeout for the process.</param>
    /// <returns>The fault detector.</returns>
    public static IFaultDetector DefaultOutOfProcess(ProcessReference processReference, TimeSpan? timeout = null) => Create(action =>
    {
        timeout ??= TimeSpan.MaxValue;
        if (!processReference.Process.WaitForExit(timeout.Value)) return FaultResult.Timeout(timeout.Value);
        if (processReference.Process.ExitCode != 0) return FaultResult.Code(processReference.Process.ExitCode);
        return FaultResult.Ok;
    });

    /// <summary>
    /// Creates a fault detector that filters out identical traces, so they are only reported once.
    /// </summary>
    /// <param name="innerDetector">The inner fault detector.</param>
    /// <returns>The fault detector.</returns>
    public static IFaultDetector FilterIdenticalTraces(IFaultDetector innerDetector)
    {
        var exceptionCache = new HashSet<Exception>(ExceptionStackTraceEqualityComparer.Instance);

        return Create(action =>
        {
            var result = innerDetector.Execute(action);
            if (result.ThrownException is not null && !exceptionCache.Add(result.ThrownException))
            {
                // This was an exception fault and we have seen it before
                // Lie that the fault was not detected
                return FaultResult.Ok;
            }
            return result;
        });
    }

    private sealed class DelegateFaultDetector(Func<Action, FaultResult> func) : IFaultDetector
    {
        public FaultResult Execute(Action action) => func(action);
    }
}
