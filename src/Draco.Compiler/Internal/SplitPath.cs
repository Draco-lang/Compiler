using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Draco.Compiler.Internal;

/// <summary>
/// Represents parts of a path split by directory separator, excluding file names.
/// </summary>
internal readonly struct SplitPath
{
    public ReadOnlyMemory<string> Parts { get; }

    public bool IsEmpty { get; }

    /// <summary>
    /// Creates a <see cref="SplitPath"/> from file excluding file name.
    /// </summary>
    /// <param name="path">The file path from which to create <see cref="SplitPath"/>.</param>
    /// <returns>The created <see cref="SplitPath"/>.</returns>
    public static SplitPath FromFilePath(string path)
    {
        var split = path.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
        return new SplitPath(split.AsMemory()[..^1]);
    }

    /// <summary>
    /// Creates a <see cref="SplitPath"/> from a directory including every part of the path.
    /// </summary>
    /// <param name="path">The direcotry path from which to create <see cref="SplitPath"/>.</param>
    /// <returns>The created <see cref="SplitPath"/>.</returns>
    public static SplitPath FromDirectoryPath(string path)
    {
        var split = path.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
        return new SplitPath(split.AsMemory());
    }

    public SplitPath(ReadOnlyMemory<string> path)
    {
        this.Parts = path;
        this.IsEmpty = this.Parts.Length == 1 && string.IsNullOrEmpty(this.Parts.Span[0]);
    }

    /// <summary>
    /// Removes a <paramref name="prefix"/> from this path.
    /// </summary>
    /// <param name="prefix">The prefix that will be removed.</param>
    /// <returns>The new created <see cref="SplitPath"/> without the <paramref name="prefix"/>.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public SplitPath RemovePrefix(SplitPath prefix)
    {
        if (!this.StartsWith(prefix)) throw new InvalidOperationException();
        return new SplitPath(this.Parts[prefix.Parts.Length..]);
    }

    /// <summary>
    /// Determines if this path starts with <paramref name="other"/> <see cref="SplitPath"/>.
    /// </summary>
    /// <param name="other">The other <see cref="SplitPath"/> this path should start with.</param>
    /// <returns>True, if this path starts with <paramref name="other"/>, otherwise false.</returns>
    public bool StartsWith(SplitPath other) => this.Parts.Span.StartsWith(other.Parts.Span);
}
