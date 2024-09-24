using System;
using System.Collections.Generic;
using System.Threading;

namespace Draco.Fuzzing;

/// <summary>
/// Detects faults (crashes, exceptions, timeouts, ...) in the target to be executed.
/// </summary>
public interface IFaultDetector
{
    /// <summary>
    /// Runs the target and detects faults.
    /// </summary>
    /// <param name="targetExecutor">The target executor.</param>
    /// <param name="targetInfo">The target information to run the executor with.</param>
    /// <returns>The fault result.</returns>
    public FaultResult Detect(ITargetExecutor targetExecutor, TargetInfo targetInfo);
}

/// <summary>
/// Factory for common fault detection logic.
/// </summary>
public static class FaultDetector
{
    /// <summary>
    /// Creates a fault detector for in-process execution.
    /// </summary>
    /// <param name="timeout">The timeout for the execution.</param>
    /// <returns>The fault detector.</returns>
    public static IFaultDetector InProcess(TimeSpan? timeout = null) => new InProcessDetector(timeout);

    /// <summary>
    /// Creates a fault detector for out-of-process execution.
    /// </summary>
    /// <param name="timeout">The timeout for the execution.</param>
    /// <returns>The fault detector.</returns>
    public static IFaultDetector OutOfProcess(TimeSpan? timeout = null) => new OutOfProcessDetector(timeout);

    /// <summary>
    /// Creates a fault detector that filters out identical traces.
    /// </summary>
    /// <param name="innerDetector">The inner detector to detect with.</param>
    /// <returns>The fault detector.</returns>
    public static IFaultDetector FilterIdenticalTraces(IFaultDetector innerDetector) => new FilterIdenticalTracesDetector(innerDetector);

    private sealed class InProcessDetector(TimeSpan? timeout) : IFaultDetector
    {
        private readonly TimeSpan timeout = timeout ?? TimeSpan.MaxValue;

        public FaultResult Detect(ITargetExecutor targetExecutor, TargetInfo targetInfo)
        {
            var exception = null as Exception;
            var evt = new ManualResetEvent(false);
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    targetExecutor.Execute(targetInfo);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                evt.Set();
            });
            if (!evt.WaitOne(this.timeout)) return FaultResult.Timeout(this.timeout);
            if (exception is not null) return FaultResult.Exception(exception);
            return FaultResult.Ok;
        }
    }

    private sealed class OutOfProcessDetector(TimeSpan? timeout) : IFaultDetector
    {
        private readonly TimeSpan timeout = timeout ?? TimeSpan.MaxValue;

        public FaultResult Detect(ITargetExecutor targetExecutor, TargetInfo targetInfo)
        {
            if (targetInfo.Process is null)
            {
                throw new ArgumentException("target information does not contain a process to run", nameof(targetInfo));
            }

            // TODO: We could definitely capture the STDERR for extra info
            targetInfo.Process.Start();
            if (!targetInfo.Process.WaitForExit(this.timeout)) return FaultResult.Timeout(this.timeout);
            if (targetInfo.Process.ExitCode != 0) return FaultResult.Code(targetInfo.Process.ExitCode);
            return FaultResult.Ok;
        }
    }

    private sealed class FilterIdenticalTracesDetector(IFaultDetector innerDetector) : IFaultDetector
    {
        private readonly HashSet<Exception> exceptionCache = new(ExceptionStackTraceEqualityComparer.Instance);

        public FaultResult Detect(ITargetExecutor targetExecutor, TargetInfo targetInfo)
        {
            var result = innerDetector.Detect(targetExecutor, targetInfo);
            if (result.ThrownException is not null && !this.exceptionCache.Add(result.ThrownException))
            {
                // This was an exception fault and we have seen it before
                // Lie that the fault was not detected
                return FaultResult.Ok;
            }
            return result;
        }
    }
}
