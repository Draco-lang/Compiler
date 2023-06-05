using System;
using System.Runtime.InteropServices;
using System.Text;
using Draco.Debugger.IO;

namespace Draco.Debugger.Platform;

/// <summary>
/// Implements <see cref="ReplaceStdioHandles"/> for Windows.
/// </summary>
internal sealed class Win32PlatformMethods : IPlatformMethods
{
    private enum StandardHandleType : uint
    {
        STD_INPUT_HANDLE = 4294967286,
        STD_OUTPUT_HANDLE = 4294967285,
        STD_ERROR_HANDLE = 4294967284,
    }

    private const string Kernel32 = "kernel32.dll";

    private static readonly nint INVALID_HANDLE_VALUE = new(-1);

    [DllImport(Kernel32, SetLastError = true)]
    private static extern nint GetStdHandle(StandardHandleType nStdHandle);

    [DllImport(Kernel32, SetLastError = true)]
    private static extern bool SetStdHandle(StandardHandleType nStdHandle, nint hHandle);

    [DllImport(Kernel32, SetLastError = true)]
    private static extern int GetThreadDescription(IntPtr hThread, out IntPtr result);

    [DllImport(Kernel32, SetLastError = true)]
    private static extern IntPtr LocalFree(IntPtr hMem);

    private static nint ReplaceStdioHandle(StandardHandleType old, nint @new)
    {
        var oldCopy = GetStdHandle(old);
        if (oldCopy == INVALID_HANDLE_VALUE) throw new InvalidOperationException($"could not get {old} handle");

        if (!SetStdHandle(old, @new)) throw new InvalidOperationException($"could not set {@old} handle");

        return oldCopy;
    }

    public IoHandles ReplaceStdioHandles(IoHandles newHandles)
    {
        var oldStdin = ReplaceStdioHandle(StandardHandleType.STD_INPUT_HANDLE, newHandles.StandardInput);
        var oldStdout = ReplaceStdioHandle(StandardHandleType.STD_OUTPUT_HANDLE, newHandles.StandardOutput);
        var oldStderr = ReplaceStdioHandle(StandardHandleType.STD_ERROR_HANDLE, newHandles.StandardError);

        return new(oldStdin, oldStdout, oldStderr);
    }

    public string? GetThreadName(nint threadId)
    {
        var success = GetThreadDescription(threadId, out var name);
        if (success < 0) throw new InvalidOperationException($"could not retrieve thread name of {threadId}");

        var result = Marshal.PtrToStringUni(name);

        var freed = LocalFree(name);
        if (freed != IntPtr.Zero) throw new InvalidOperationException("failed to free memory");

        return result;
    }
}
