using Draco.Debugger.IO;

namespace Draco.Debugger.Platform;

/// <summary>
/// Platform abstractions for the different supported OSes.
/// </summary>
internal interface IPlatformMethods
{
    /// <summary>
    /// Replaces the standard IO handles for this process.
    /// </summary>
    /// <param name="newHandles">The new handles to be used by the process.</param>
    /// <returns>The old IO handles.</returns>
    public IoHandles ReplaceStdioHandles(IoHandles newHandles);
}
