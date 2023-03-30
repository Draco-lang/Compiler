using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Fuzzer;

/// <summary>
/// Represents an exception coming from an incremental change.
/// </summary>
internal sealed class MutationException : Exception
{
    /// <summary>
    /// The input that was being fed into the component previously.
    /// </summary>
    public string OldInput { get; }

    /// <summary>
    /// The input that was fed into the compiler next, causing the crash.
    /// </summary>
    public string NewInput { get; }

    /// <summary>
    /// The original exception.
    /// </summary>
    public Exception OriginalException { get; }

    public MutationException(string oldInput, string newInput, Exception originalException)
    {
        this.OldInput = oldInput;
        this.NewInput = newInput;
        this.OriginalException = originalException;
    }
}
