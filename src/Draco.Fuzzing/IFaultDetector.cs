using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

            // Make sure the process is disposed at the end
            using var process = targetInfo.Process;

            process.StartInfo.RedirectStandardError = true;
            targetExecutor.Execute(targetInfo);

            using var cancelReadSource = new CancellationTokenSource();
            var stderrTask = ReadStream(process.StandardError, cancelReadSource.Token);

            if (!process.WaitForExit(this.timeout))
            {
                cancelReadSource.Cancel();
                process.Dispose();
                return FaultResult.Timeout(this.timeout);
            }

            var stderr = stderrTask.GetAwaiter().GetResult();
            if (process.ExitCode != 0) return FaultResult.Code(process.ExitCode, errorMessage: stderr);
            return FaultResult.Ok;
        }

        // NOTE: We need this because the output stream readers of Process absolutely SUCK
        // Our original pattern was this:
        // ```cs
        // process.Start();
        // if (!process.WaitForExit(timeout)) { /* TIMEOUT detected */ }
        // var stderr = process.StandardError.ReadToEnd();
        // ```
        // But the problem with this is that redirected output streams can get stuck if the buffer is full
        // meaning the running process can get stuck on a write operation.
        // You can't reverse the order of reading and waiting because then we can't detect an actual timeout,
        // as the ReadToEnd call will block until the process exits.
        // We tried to launch a task ReadToEndAsync, but that doesn't work either, because the scheduler will get
        // overwhelmed with incomplete tasks after a while.
        // This is the only solution I found that can completely eliminate these fake timeouts due to blocked streams.
        // I hate you System.Diagnostics.Process, maybe forever.
        private static Task<string> ReadStream(StreamReader reader, CancellationToken cancellationToken)
        {
            var stderrSource = new TaskCompletionSource<string>();
            ThreadPool.QueueUserWorkItem(_ =>
            {
                var result = new StringBuilder();
                var block = new char[4096];
                try
                {
                    while (!cancellationToken.IsCancellationRequested && !reader.EndOfStream)
                    {
                        var read = reader.ReadBlock(block, 0, block.Length);
                        result.Append(block, 0, read);
                    }
                }
                finally
                {
                    stderrSource.SetResult(result.ToString());
                }
            });
            return stderrSource.Task;
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
