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
        STDERR_FILENO = 2,
    }

    private const string LibC = "libc";

    [DllImport(LibC, SetLastError = true)]
    private static extern int dup(int oldfd);

    [DllImport(LibC, SetLastError = true)]
    private static extern int dup2(int oldfd, int newfd);

    public IoHandles GetStdioHandles()
    {
        // Duplicate the existing file descriptors to get their handles
        var stdin = (IntPtr)dup((int)FileDescriptor.STDIN_FILENO);
        var stdout = (IntPtr)dup((int)FileDescriptor.STDOUT_FILENO);
        var stderr = (IntPtr)dup((int)FileDescriptor.STDERR_FILENO);
        //var stdin = (IntPtr)FileDescriptor.STDIN_FILENO; // investigate why this doesn't works.
        //var stdout = (IntPtr)FileDescriptor.STDOUT_FILENO;
        //var stderr = (IntPtr)FileDescriptor.STDERR_FILENO;
        return new IoHandles(stdin, stdout, stderr);
    }

    public void SetStdioHandles(IoHandles handles)
    {
        // Replace the standard file descriptors with the provided handles
        if (dup2(handles.StandardInput.ToInt32(), (int)FileDescriptor.STDIN_FILENO) == -1) throw new InvalidOperationException($"could not set {handles.StandardInput} handle");
        if (dup2(handles.StandardOutput.ToInt32(), (int)FileDescriptor.STDOUT_FILENO) == -1) throw new InvalidOperationException($"could not set {handles.StandardOutput} handle");
        if (dup2(handles.StandardError.ToInt32(), (int)FileDescriptor.STDERR_FILENO) == -1) throw new InvalidOperationException($"could not set {handles.StandardError} handle");
    }
}
