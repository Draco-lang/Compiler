namespace Draco.Coverage.Platform;

internal readonly unsafe record struct BufferHandle(
    nint NativeHandle,
    nint Buffer,
    string Name);
