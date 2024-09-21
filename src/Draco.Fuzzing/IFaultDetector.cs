using System;
using System.Threading;
using System.Threading.Tasks;

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
    /// Creates a default fault detector that catches exceptions and timeouts.
    /// </summary>
    /// <param name="timeout">The timeout for the action.</param>
    /// <returns>The fault detector.</returns>
    public static IFaultDetector Default(TimeSpan? timeout = null) => new DefaultFaultDetector(timeout ?? TimeSpan.MaxValue);

    private sealed class DefaultFaultDetector(TimeSpan timeout) : IFaultDetector
    {
        public FaultResult Execute(Action action)
        {
            try
            {
                var evt = new ManualResetEvent(false);
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    action();
                    evt.Set();
                });
                if (!evt.WaitOne(1000)) return FaultResult.Timeout(timeout);
                return FaultResult.Ok;
            }
            catch (Exception ex)
            {
                return FaultResult.Exception(ex);
            }
        }
    }
}
