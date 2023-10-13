namespace Draco.Compiler.Fuzzer;

/// <summary>
/// Represents an exception coming from a non-incremental change.
/// </summary>
internal sealed class CrashException : Exception
{
    /// <summary>
    /// The input that was being fed into the component.
    /// </summary>
    public string Input { get; }

    /// <summary>
    /// The original exception.
    /// </summary>
    public Exception OriginalException { get; }

    public CrashException(string input, Exception originalException)
    {
        this.Input = input;
        this.OriginalException = originalException;
    }
}
