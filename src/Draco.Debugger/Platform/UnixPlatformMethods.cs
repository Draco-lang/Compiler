using System;
using System.Runtime.InteropServices;
using Draco.Debugger.IO;

namespace Draco.Debugger.Platform;

/// <summary>
/// Implements <see cref="IPlatformMethods"/> for Unix systems.
/// </summary>
internal sealed class UnixPlatformMethods : IPlatformMethods
{
    private enum FileDescriptor : int
    {
        STDIN_FILENO = 0,
        STDOUT_FILENO = 1,
        STDERR_FILENO = 2
    }

    private const string LibC = "libc";

    [DllImport(LibC, SetLastError = true)]
    private static extern int dup(int oldfd);

    [DllImport(LibC, SetLastError = true)]
    private static extern int dup2(int oldfd, int newfd);

    private static nint ReplaceStdioHandle(FileDescriptor old, nint @new)
    {
        var oldCopy = dup((int)old);
        if (oldCopy == -1) throw new InvalidOperationException($"could not get {old} handle");

        var newCopy = dup2((int)@new, (int)old);
        if (newCopy == -1) throw new InvalidOperationException($"could not set {old} handle");

        return oldCopy;
    }

    public IoHandles ReplaceStdioHandles(IoHandles newHandles)
    {
        var oldStdin = ReplaceStdioHandle(FileDescriptor.STDIN_FILENO, newHandles.StandardInput);
        var oldStdout = ReplaceStdioHandle(FileDescriptor.STDOUT_FILENO, newHandles.StandardOutput);
        var oldStderr = ReplaceStdioHandle(FileDescriptor.STDERR_FILENO, newHandles.StandardError);

        return new IoHandles(oldStdin, oldStdout, oldStderr);
    }
}
