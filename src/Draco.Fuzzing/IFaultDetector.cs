using System;

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
