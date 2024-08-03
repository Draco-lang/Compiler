namespace Draco.Compiler.Fuzzer;

/// <summary>
/// Represents an exception coming from an incremental change.
/// </summary>
internal sealed class MutationException(
    string oldInput,
    string newInput,
    Exception originalException) : Exception
{
    /// <summary>
    /// The input that was being fed into the component previously.
    /// </summary>
    public string OldInput { get; } = oldInput;

    /// <summary>
    /// The input that was fed into the compiler next, causing the crash.
    /// </summary>
    public string NewInput { get; } = newInput;

    /// <summary>
    /// The original exception.
    /// </summary>
    public Exception OriginalException { get; } = originalException;
}
