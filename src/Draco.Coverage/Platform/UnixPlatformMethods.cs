using System;
using System.Runtime.InteropServices;

namespace Draco.Coverage.Platform;

/// <summary>
/// Platform-specific methods for Unix.
/// </summary>
internal sealed class UnixPlatformMethods : IPlatformMethods
{
    private const string LibC = "libc";

    private const int O_CREAT = 64;
    private const int O_RDWR = 2;

    private const int S_IRUSR = 256;
    private const int S_IWUSR = 128;

    private const int PROT_READ = 1;
    private const int PROT_WRITE = 2;

    private const int MAP_SHARED = 1;

    [DllImport(LibC, SetLastError = true)]
    private static extern int shm_open(string name, int oflag, int mode);

    [DllImport(LibC, SetLastError = true)]
    private static extern int shm_unlink(string name);

    [DllImport(LibC, SetLastError = true)]
    private static extern int ftruncate(int fd, int length);

    [DllImport(LibC, SetLastError = true)]
    private static extern nint mmap(nint addr, int len, int prot, int flags, int fd, int offset);

    [DllImport(LibC, SetLastError = true)]
    private static extern int munmap(nint addr, int len);

    public BufferHandle CreateNewSharedMemoryBuffer(string name, int size)
    {
        var desc = shm_open(name, O_CREAT | O_RDWR, S_IRUSR | S_IWUSR);
        if (desc == -1)
        {
            throw new InvalidOperationException("could not allocate shared memory buffer");
        }

        if (ftruncate(desc, size) == -1)
        {
            throw new InvalidOperationException("could not resize shared memory buffer");
        }

        var buffer = mmap(nint.Zero, size, PROT_READ | PROT_WRITE, MAP_SHARED, desc, 0);
        if (buffer == nint.Zero)
        {
            throw new InvalidOperationException("could not map shared memory buffer");
        }

        return new(desc, buffer, name);
    }

    public BufferHandle OpenExistingSharedMemoryBuffer(string name, int size)
    {
        var desc = shm_open(name, O_RDWR, 0);
        if (desc == -1)
        {
            throw new InvalidOperationException("could not open shared memory buffer");
        }

        var buffer = mmap(nint.Zero, size, PROT_READ | PROT_WRITE, MAP_SHARED, desc, 0);
        if (buffer == nint.Zero)
        {
            throw new InvalidOperationException("could not map shared memory buffer");
        }

        return new(desc, buffer, name);
    }

    public void CloseSharedMemoryBuffer(BufferHandle handle)
    {
        munmap(handle.Buffer, 0);
        shm_unlink(handle.Name);
    }
}
