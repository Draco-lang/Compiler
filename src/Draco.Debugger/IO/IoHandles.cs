using System;

namespace Draco.Debugger.IO;

/// <summary>
/// A triplet of all STDIO handles for a process.
/// </summary>
/// <param name="StandardInput">The standard input handle.</param>
/// <param name="StandardOutput">The standard output handle.</param>
/// <param name="StandardError">The standard error handle.</param>
internal readonly record struct IoHandles(
    IntPtr StandardInput,
    IntPtr StandardOutput,
    IntPtr StandardError);
