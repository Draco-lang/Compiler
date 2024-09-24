using System;

namespace Draco.Fuzzing;

/// <summary>
/// Represents the result of a fault detection.
/// </summary>
public readonly struct FaultResult
{
    public static readonly FaultResult Ok = default;
    public static FaultResult Timeout(TimeSpan timeout) => new()
    {
        TimeoutReached = timeout,
    };
    public static FaultResult Exception(Exception exception) => new()
    {
        ThrownException = exception,
    };
    public static FaultResult Code(int exitCode, string? errorMessage = null) => new()
    {
        ExitCode = exitCode,
        ErrorMessage = errorMessage,
    };
    public static FaultResult Error(string errorMessage) => new()
    {
        ErrorMessage = errorMessage,
    };

    /// <summary>
    /// True, if the target is considered as faulted.
    /// </summary>
    public bool IsFaulted => this.TimeoutReached is not null
                          || this.ThrownException is not null
                          || this.ExitCode != 0
                          || this.ErrorMessage is not null;

    /// <summary>
    /// If not null, the timeout was reached for the given time span.
    /// </summary>
    public TimeSpan? TimeoutReached { get; init; }

    /// <summary>
    /// If not null, the target threw an exception.
    /// </summary>
    public Exception? ThrownException { get; init; }

    /// <summary>
    /// The exit code of the target.
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>
    /// Additional error message.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
