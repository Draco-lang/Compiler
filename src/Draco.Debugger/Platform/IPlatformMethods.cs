using Draco.Debugger.IO;

namespace Draco.Debugger.Platform;

/// <summary>
/// Platform abstractions for the different supported OSes.
/// </summary>
internal interface IPlatformMethods
{
    /// <summary>
    /// Gets the standard IO handles of the current process.
    /// </summary>
    /// <returns></returns>
    public IoHandles GetStdioHandles();
    /// <summary>
    /// Sets the standard IO handles of the current process.
    /// </summary>
    /// <param name="handles"></param>
    public void SetStdioHandles(IoHandles handles);
}
