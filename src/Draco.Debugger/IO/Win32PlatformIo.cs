using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Debugger.IO;

/// <summary>
/// Implements <see cref="IPlatformIo"/> for Windows.
/// </summary>
internal sealed class Win32PlatformIo : IPlatformIo
{
    private enum StandardHandleType : uint
    {
        STD_INPUT_HANDLE = 4294967286,
        STD_OUTPUT_HANDLE = 4294967285,
        STD_ERROR_HANDLE = 4294967284,
    }

    private const string Kernel32 = "kernel32.dll";

    private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

    [DllImport(Kernel32, SetLastError = true)]
    private static extern IntPtr GetStdHandle(StandardHandleType nStdHandle);

    [DllImport(Kernel32, SetLastError = true)]
    private static extern bool SetStdHandle(StandardHandleType nStdHandle, IntPtr hHandle);

    private static IntPtr Replace(StandardHandleType old, IntPtr @new)
    {
        var oldCopy = GetStdHandle(old);
        if (oldCopy == INVALID_HANDLE_VALUE) throw new InvalidOperationException($"could not get {old} handle");

        if (!SetStdHandle(old, @new)) throw new InvalidOperationException($"could not set {@old} handle");

        return oldCopy;
    }

    public IoHandles Replace(IoHandles newHandles)
    {
        var oldStdin = Replace(StandardHandleType.STD_INPUT_HANDLE, newHandles.StandardInput);
        var oldStdout = Replace(StandardHandleType.STD_OUTPUT_HANDLE, newHandles.StandardOutput);
        var oldStderr = Replace(StandardHandleType.STD_ERROR_HANDLE, newHandles.StandardError);

        return new(oldStdin, oldStdout, oldStderr);
    }
}
