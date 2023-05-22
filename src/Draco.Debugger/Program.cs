using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Debugger;

internal static class Win32
{
    private const string kernel32 = "kernel32.dll";

    [DllImport(kernel32, SetLastError = true)]
    public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    [DllImport(kernel32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "LoadLibraryW")]
    public static extern IntPtr LoadLibrary(string lpLibFileName);

    [DllImport(kernel32, SetLastError = true)]
    public static extern uint WaitForSingleObject([In] IntPtr hHandle, [In] int dwMilliseconds);

    [DllImport(kernel32, SetLastError = true)]
    public static extern bool SetEvent([In] IntPtr hEvent);
}

internal static class Program
{
    private sealed class NativeMethods : INativeMethods
    {
        public nint LoadLibrary(string path) => Win32.LoadLibrary(path);
    }

    internal static async Task Main(string[] args)
    {
        var host = DebuggerHost.Create(new NativeMethods(), FindDbgShim());
        var debugger = await host.StartProcess("c:/TMP/DracoTest/bin/Debug/net7.0/DracoTest.exe");

        debugger.SetBreakpoint(100663297, 0x0c);
        debugger.Resume();

        foreach (var (uri, file) in debugger.SourceFiles)
        {
            Console.WriteLine(file.Text);
        }

        await Task.Delay(1000);

        debugger.Resume();

        await debugger.Terminated;
    }

    private static string FindDbgShim()
    {
        var root = "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App";

        if (!Directory.Exists(root))
            throw new InvalidOperationException($"Cannot find dbgshim.dll: '{root}' does not exist");

        foreach (var dir in Directory.EnumerateDirectories(root).Reverse())
        {
            var dbgshim = Directory.EnumerateFiles(dir, "dbgshim.dll").FirstOrDefault();

            if (dbgshim != null)
                return dbgshim;
        }

        throw new InvalidOperationException($"Failed to find a runtime containing dbgshim.dll under '{root}'");
    }
}
