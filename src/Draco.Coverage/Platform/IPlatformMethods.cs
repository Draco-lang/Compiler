namespace Draco.Coverage.Platform;

/// <summary>
/// Platform abstractions for the different supported OSes.
/// </summary>
internal interface IPlatformMethods
{
    /// <summary>
    /// Creates a shared memory buffer with the given name and size.
    /// </summary>
    /// <param name="name">The name of the shared memory buffer.</param>
    /// <param name="size">The size of the shared memory buffer.</param>
    /// <returns>The pointer to the shared memory buffer.</returns>
    public BufferHandle CreateNewSharedMemoryBuffer(string name, int size);

    /// <summary>
    /// Opens an existing shared memory buffer with the given name.
    /// </summary>
    /// <param name="name">The name of the shared memory buffer.</param>
    /// <param name="size">The size of the shared memory buffer.</param>
    /// <returns>The pointer to the shared memory buffer.</returns>
    public BufferHandle OpenExistingSharedMemoryBuffer(string name, int size);

    /// <summary>
    /// Closes the shared memory buffer with the given name.
    /// </summary>
    /// <param name="handle">The handle to the shared memory buffer.</param>
    /// <param name="name">The name of the shared memory buffer.</param>
    public void CloseSharedMemoryBuffer(BufferHandle handle);
}
