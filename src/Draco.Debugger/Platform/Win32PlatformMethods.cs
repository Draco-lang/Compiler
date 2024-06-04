using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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

    public IoHandles GetStdioHandles()
    {
        var stdin = GetStdHandle(StandardHandleType.STD_INPUT_HANDLE);
        if (stdin == INVALID_HANDLE_VALUE) throw new InvalidOperationException($"could not get {stdin} handle");
        var stdout = GetStdHandle(StandardHandleType.STD_OUTPUT_HANDLE);
        if (stdout == INVALID_HANDLE_VALUE) throw new InvalidOperationException($"could not get {stdout} handle");
        var stderr = GetStdHandle(StandardHandleType.STD_ERROR_HANDLE);
        if (stderr == INVALID_HANDLE_VALUE) throw new InvalidOperationException($"could not get {stderr} handle");

        return new(stdin, stdout, stderr);
    }

    public void SetStdioHandles(IoHandles handles)
    {
        if (!SetStdHandle(StandardHandleType.STD_INPUT_HANDLE, handles.StandardInput)) throw new InvalidOperationException($"could not set {handles.StandardInput} handle");
        if (!SetStdHandle(StandardHandleType.STD_OUTPUT_HANDLE, handles.StandardOutput)) throw new InvalidOperationException($"could not set {handles.StandardOutput} handle");
        if (!SetStdHandle(StandardHandleType.STD_ERROR_HANDLE, handles.StandardError)) throw new InvalidOperationException($"could not set {handles.StandardError} handle");
    }
}
