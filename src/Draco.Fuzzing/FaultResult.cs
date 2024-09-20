using System;

namespace Draco.Fuzzing;

/// <summary>
/// Represents the result of a fault detection.
/// </summary>
public readonly struct FaultResult
{
    /// <summary>
    /// True, if the target is considered as faulted.
    /// </summary>
    public bool IsFaulted => this.TimeoutReached is not null
                          || this.ThrownException is not null;

    /// <summary>
    /// If not null, the timeout was reached for the given time span.
    /// </summary>
    public TimeSpan? TimeoutReached { get; }

    /// <summary>
    /// If not null, the target threw an exception.
    /// </summary>
    public Exception? ThrownException { get; }

    internal FaultResult(TimeSpan? timeoutReached, Exception? thrownException)
    {
        this.TimeoutReached = timeoutReached;
        this.ThrownException = thrownException;
    }
}
