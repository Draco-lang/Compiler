using System;
using System.IO.Pipes;
using System.IO;
using System.Runtime.InteropServices;

namespace Draco.Debugger.IO;

/// <summary>
/// Cross-platform IO utilities.
/// </summary>
internal static class IoUtils
{
    private static bool IsUnixPlatform() =>
           RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
        || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
        || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);

    /// <summary>
    /// Retrieves an <see cref="IPlatformIo"/> implementation for this platform.
    /// </summary>
    /// <returns>The <see cref="IPlatformIo"/> implementation that is supported on the current platform.</returns>
    public static IPlatformIo GetPlatformIo()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return new Win32PlatformIo();
        if (IsUnixPlatform()) return new UnixPlatformIo();

        throw new NotSupportedException($"no IO utilities are implemented for this platform");
    }

    /// <summary>
    /// Captures the IO handles of a started process.
    /// </summary>
    /// <typeparam name="TResult">The result the process startup routine returns.</typeparam>
    /// <param name="startProcess">The routine that starts the process.</param>
    /// <param name="ioHandles">The IO handles of the started up process are written here.</param>
    /// <returns>The result returned by <paramref name="startProcess"/>.</returns>
    public static TResult CaptureProcess<TResult>(Func<TResult> startProcess, out RemoteIoHandles ioHandles)
    {
        var stdinLocal = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
        var stdinRemote = new AnonymousPipeClientStream(PipeDirection.In, stdinLocal.ClientSafePipeHandle);

        var stdoutLocal = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
        var stdoutRemote = new AnonymousPipeClientStream(PipeDirection.Out, stdoutLocal.ClientSafePipeHandle);

        var stderrLocal = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
        var stderrRemote = new AnonymousPipeClientStream(PipeDirection.Out, stderrLocal.ClientSafePipeHandle);

        var platformIo = GetPlatformIo();
        var pipeHandles = new IoHandles(
            StandardInput: stdinRemote.SafePipeHandle.DangerousGetHandle(),
            StandardOutput: stdoutRemote.SafePipeHandle.DangerousGetHandle(),
            StandardError: stderrRemote.SafePipeHandle.DangerousGetHandle());

        var oldHandles = platformIo.Replace(pipeHandles);

        var processResult = startProcess();

        stdinLocal.DisposeLocalCopyOfClientHandle();
        stdoutLocal.DisposeLocalCopyOfClientHandle();
        stderrLocal.DisposeLocalCopyOfClientHandle();

        platformIo.Replace(oldHandles);

        Console.SetIn(new StreamReader(Console.OpenStandardInput()));
        Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });

        ioHandles = new(
            StandardInputWriter: stdinLocal,
            StandardOutputReader: stdoutLocal,
            StandardErrorReader: stderrLocal);

        return processResult;
    }
}
