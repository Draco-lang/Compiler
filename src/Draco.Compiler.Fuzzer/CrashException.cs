namespace Draco.Compiler.Fuzzer;

/// <summary>
/// Represents an exception coming from a non-incremental change.
/// </summary>
internal sealed class CrashException(string input, Exception originalException) : Exception
{
    /// <summary>
    /// The input that was being fed into the component.
    /// </summary>
    public string Input { get; } = input;

    /// <summary>
    /// The original exception.
    /// </summary>
    public Exception OriginalException { get; } = originalException;
}
