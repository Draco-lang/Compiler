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
