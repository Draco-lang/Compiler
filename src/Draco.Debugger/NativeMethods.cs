using System;
using System.Runtime.InteropServices;

namespace Draco.Debugger;

/// <summary>
/// Native methods needed by the debugger.
/// </summary>
internal static class NativeMethods
{
    private const string kernel32 = "kernel32.dll";

    [DllImport(kernel32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "LoadLibraryW")]
    public static extern IntPtr LoadLibrary(string lpLibFileName);
}
