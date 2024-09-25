using System;
using System.Runtime.InteropServices;

namespace Draco.Coverage.Platform;

/// <summary>
/// Implements platform-specific methods for Windows.
/// </summary>
internal sealed class Win32PlatformMethods : IPlatformMethods
{
    private const string Kernel32 = "kernel32.dll";

    private const int PAGE_READWRITE = 0x04;
    private const int FILE_MAP_ALL_ACCESS = 0xF001F;

    private static readonly nint INVALID_HANDLE_VALUE = new(-1);

    [DllImport(Kernel32, SetLastError = true)]
    private static extern nint CreateFileMappingA(
        nint hFile,
        nint lpAttributes,
        int flProtect,
        uint dwMaximumSizeHigh,
        uint dwMaximumSizeLow,
        string lpName);

    [DllImport(Kernel32, SetLastError = true)]
    private static extern nint OpenFileMappingA(
        uint dwDesiredAccess,
        bool bInheritHandle,
        string lpName);

    [DllImport(Kernel32, SetLastError = true)]
    private static extern nint MapViewOfFile(
        nint hFileMappingObject,
        uint dwDesiredAccess,
        uint dwFileOffsetHigh,
        uint dwFileOffsetLow,
        uint dwNumberOfBytesToMap);

    [DllImport(Kernel32, SetLastError = true)]
    private static extern bool UnmapViewOfFile(nint lpBaseAddress);

    [DllImport(Kernel32, SetLastError = true)]
    private static extern bool CloseHandle(nint hObject);

    public unsafe BufferHandle CreateNewSharedMemoryBuffer(string name, int size)
    {
        var handle = CreateFileMappingA(INVALID_HANDLE_VALUE, nint.Zero, PAGE_READWRITE, 0, (uint)size, name);
        if (handle == nint.Zero)
        {
            throw new InvalidOperationException("could not allocate shared memory buffer");
        }

        var buffer = MapViewOfFile(handle, FILE_MAP_ALL_ACCESS, 0, 0, (uint)size);
        if (buffer == nint.Zero)
        {
            throw new InvalidOperationException("could not map shared memory buffer");
        }

        return new(handle, buffer, name);
    }

    public unsafe BufferHandle OpenExistingSharedMemoryBuffer(string name, int size)
    {
        var handle = OpenFileMappingA(FILE_MAP_ALL_ACCESS, false, name);
        if (handle == nint.Zero)
        {
            throw new InvalidOperationException("could not open shared memory buffer");
        }

        var buffer = MapViewOfFile(handle, FILE_MAP_ALL_ACCESS, 0, 0, (uint)size);
        if (buffer == nint.Zero)
        {
            throw new InvalidOperationException("could not map shared memory buffer");
        }

        return new(handle, buffer, name);
    }

    public unsafe void CloseSharedMemoryBuffer(BufferHandle handle)
    {
        UnmapViewOfFile(new nint(handle.Buffer));
        CloseHandle(handle.NativeHandle);
    }
}
