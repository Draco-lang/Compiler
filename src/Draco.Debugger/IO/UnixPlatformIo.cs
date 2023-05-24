using System;
using System.Runtime.InteropServices;

namespace Draco.Debugger.IO;

/// <summary>
/// Implements <see cref="IPlatformIo"/> for Unix systems.
/// </summary>
internal sealed class UnixPlatformIo : IPlatformIo
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

    private static nint Replace(FileDescriptor old, nint @new)
    {
        var oldCopy = dup((int)old);
        if (oldCopy == -1) throw new InvalidOperationException($"could not get {old} handle");

        var newCopy = dup2((int)@new, (int)old);
        if (newCopy == -1) throw new InvalidOperationException($"could not set {old} handle");

        return oldCopy;
    }

    public IoHandles Replace(IoHandles newHandles)
    {
        var oldStdin = Replace(FileDescriptor.STDIN_FILENO, newHandles.StandardInput);
        var oldStdout = Replace(FileDescriptor.STDOUT_FILENO, newHandles.StandardOutput);
        var oldStderr = Replace(FileDescriptor.STDERR_FILENO, newHandles.StandardError);

        return new IoHandles(oldStdin, oldStdout, oldStderr);
    }
}
