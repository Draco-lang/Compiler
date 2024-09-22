using System;

namespace Draco.Fuzzing;

/// <summary>
/// Represents the result of a fault detection.
/// </summary>
public readonly struct FaultResult
{
    public static readonly FaultResult Ok = new(timeoutReached: null, thrownException: null, exitCode: 0);
    public static FaultResult Timeout(TimeSpan timeout) => new(timeout, thrownException: null, exitCode: 0);
    public static FaultResult Exception(Exception exception) => new(timeoutReached: null, exception, exitCode: 0);
    public static FaultResult Code(int exitCode) => new(timeoutReached: null, thrownException: null, exitCode: exitCode);

    /// <summary>
    /// True, if the target is considered as faulted.
    /// </summary>
    public bool IsFaulted => this.TimeoutReached is not null
                          || this.ThrownException is not null
                          || this.ExitCode != 0;

    /// <summary>
    /// If not null, the timeout was reached for the given time span.
    /// </summary>
    public TimeSpan? TimeoutReached { get; }

    /// <summary>
    /// If not null, the target threw an exception.
    /// </summary>
    public Exception? ThrownException { get; }

    /// <summary>
    /// The exit code of the target.
    /// </summary>
    public int ExitCode { get; }

    internal FaultResult(TimeSpan? timeoutReached, Exception? thrownException, int exitCode)
    {
        this.TimeoutReached = timeoutReached;
        this.ThrownException = thrownException;
        this.ExitCode = exitCode;
    }
}
