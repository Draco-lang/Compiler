using System.Runtime.InteropServices;
using System;

namespace Draco.Coverage.Platform;

/// <summary>
/// Platform utilities.
/// </summary>
internal static class PlatformUtils
{
    /// <summary>
    /// The platform methods for the current platform.
    /// </summary>
    public static IPlatformMethods Methods { get; } = GetPlatformMethods();

    /// <summary>
    /// Checks if the current OS is a UNIX system.
    /// </summary>
    /// <returns>True, if the current OS is a UNIX system.</returns>
    public static bool IsUnixPlatform() =>
           RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
        || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
        || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);

    /// <summary>
    /// Retrieves an <see cref="IPlatformMethods"/> implementation for this platform.
    /// </summary>
    /// <returns>The <see cref="IPlatformMethods"/> implementation that is supported on the current platform.</returns>
    private static IPlatformMethods GetPlatformMethods()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return new Win32PlatformMethods();
        if (IsUnixPlatform()) return new UnixPlatformMethods();

        throw new NotSupportedException($"no IO utilities are implemented for this platform");
    }
}
