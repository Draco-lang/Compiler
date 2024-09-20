using System;
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
    public static IFaultDetector Default(TimeSpan? timeout) => new DefaultFaultDetector(timeout ?? TimeSpan.MaxValue);

    private sealed class DefaultFaultDetector(TimeSpan timeout) : IFaultDetector
    {
        public FaultResult Execute(Action action)
        {
            try
            {
                var task = Task.Run(action);
                if (task.Wait(timeout))
                {
                    return FaultResult.Ok;
                }
                else
                {
                    return FaultResult.Timeout(timeout);
                }
            }
            catch (Exception ex)
            {
                return FaultResult.Exception(ex);
            }
        }
    }
}
