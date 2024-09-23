using System;
using Draco.Coverage.Platform;

namespace Draco.Coverage;

/// <summary>
/// Factory methods for creating shared memory buffers.
/// </summary>
public static class SharedMemory
{
    /// <summary>
    /// Creates a new shared memory buffer.
    /// </summary>
    /// <typeparam name="T">The element type of the buffer.</typeparam>
    /// <param name="name">The name of the shared memory buffer.</param>
    /// <param name="length">The length of the buffer.</param>
    /// <returns>The shared memory buffer.</returns>
    public static SharedMemory<T> CreateNew<T>(string name, int length)
        where T : unmanaged =>
        SharedMemory<T>.CreateNew(name, length);

    /// <summary>
    /// Opens an existing shared memory buffer.
    /// </summary>
    /// <typeparam name="T">The element type of the buffer.</typeparam>
    /// <param name="name">The name of the shared memory buffer.</param>
    /// <param name="length">The length of the buffer.</param>
    /// <returns>The shared memory buffer.</returns>
    public static SharedMemory<T> OpenExisting<T>(string name, int length)
        where T : unmanaged =>
        SharedMemory<T>.OpenExisting(name, length);
}

/// <summary>
/// A piece of shared memory between processes.
/// </summary>
/// <typeparam name="T">The element type of the buffer.</typeparam>
public unsafe sealed class SharedMemory<T> : IDisposable
    where T : unmanaged
{
    /// <summary>
    /// Creates a new shared memory buffer.
    /// </summary>
    /// <param name="name">The name of the shared memory buffer.</param>
    /// <param name="length">The length of the buffer.</param>
    /// <returns>The shared memory buffer.</returns>
    public static SharedMemory<T> CreateNew(string name, int length)
    {
        var lengthInBytes = length * sizeof(T);
        var handle = PlatformUtils.Methods.CreateNewSharedMemoryBuffer(name, lengthInBytes);
        return new SharedMemory<T>(handle, lengthInBytes);
    }

    /// <summary>
    /// Opens an existing shared memory buffer.
    /// </summary>
    /// <param name="name">The name of the shared memory buffer.</param>
    /// <param name="length">The length of the buffer.</param>
    /// <returns>The shared memory buffer.</returns>
    public static SharedMemory<T> OpenExisting(string name, int length)
    {
        var lengthInBytes = length * sizeof(T);
        var handle = PlatformUtils.Methods.OpenExistingSharedMemoryBuffer(name, lengthInBytes);
        return new SharedMemory<T>(handle, lengthInBytes);
    }

    /// <summary>
    /// The length of the buffer.
    /// </summary>
    public readonly int Length;

    /// <summary>
    /// The span of the buffer.
    /// </summary>
    public Span<T> Span => new(this.handle.Buffer.ToPointer(), this.Length);

    private readonly BufferHandle handle;

    private SharedMemory(BufferHandle handle, int lengthInBytes)
    {
        this.handle = handle;
        this.Length = lengthInBytes / sizeof(T);
    }

    ~SharedMemory() => this.Dispose();

    public void Dispose() => PlatformUtils.Methods.CloseSharedMemoryBuffer(this.handle);
}
