namespace Draco.Debugger.IO;

/// <summary>
/// Encapsulates platform-specific IO handling.
/// </summary>
internal interface IPlatformIo
{
    /// <summary>
    /// Replaces the IO handles for this process.
    /// </summary>
    /// <param name="newHandles">The new handles to be used by the process.</param>
    /// <returns>The old IO handles.</returns>
    public IoHandles Replace(IoHandles newHandles);
}
