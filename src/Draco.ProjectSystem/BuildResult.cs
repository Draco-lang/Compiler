using System.Diagnostics.CodeAnalysis;

namespace Draco.ProjectSystem;

/// <summary>
/// Factory for creating build results.
/// </summary>
internal static class BuildResult
{
    /// <summary>
    /// Creates a successful build result.
    /// </summary>
    /// <typeparam name="T">The result type of a successful build.</typeparam>
    /// <param name="value">The result of the build.</param>
    /// <param name="log">The log of the build.</param>
    /// <returns>A successful build result.</returns>
    public static BuildResult<T> Success<T>(T value, string? log = null) => new(true, value, log);

    /// <summary>
    /// Creates a failed build result.
    /// </summary>
    /// <typeparam name="T">The result type of a successful build.</typeparam>
    /// <param name="log">The log of the build.</param>
    /// <returns>A failed build result.</returns>
    public static BuildResult<T> Failure<T>(string? log = null) => new(false, default, log);
}

/// <summary>
/// The result of a build.
/// </summary>
/// <typeparam name="T">The result type of a successful build.</typeparam>
public readonly struct BuildResult<T>(bool success, T? value, string? log = null)
{
    /// <summary>
    /// True, if the build succeeded.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool Success { get; } = success;

    /// <summary>
    /// The result of the build.
    /// </summary>
    public T? Value { get; } = value;

    /// <summary>
    /// The log of the build.
    /// </summary>
    public string Log { get; } = log ?? string.Empty;
}
