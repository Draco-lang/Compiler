using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Draco.Compiler.Internal;

/// <summary>
/// Represents parts of a path split by directory separator, excluding file names.
/// </summary>
internal readonly struct SplitPath
{
    public ReadOnlyMemory<string> Parts { get; }

    public bool IsEmpty => this.Parts.Length == 0;

    public static SplitPath Empty => new SplitPath();

    /// <summary>
    /// Creates a <see cref="SplitPath"/> from file excluding file name.
    /// </summary>
    /// <param name="path">The file path from which to create <see cref="SplitPath"/>.</param>
    /// <returns>The created <see cref="SplitPath"/>.</returns>
    public static SplitPath FromFilePath(string path)
    {
        var split = path.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
        if (split.Length == 0) return new SplitPath(ReadOnlyMemory<string>.Empty);
        return new SplitPath(split.AsMemory()[..^1]);
    }

    /// <summary>
    /// Creates a <see cref="SplitPath"/> from a directory including every part of the path.
    /// </summary>
    /// <param name="path">The direcotry path from which to create <see cref="SplitPath"/>.</param>
    /// <returns>The created <see cref="SplitPath"/>.</returns>
    public static SplitPath FromDirectoryPath(string path)
    {
        var split = path.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
        return new SplitPath(split.AsMemory());
    }

    public SplitPath(ReadOnlyMemory<string> path)
    {
        this.Parts = path;
    }

    /// <summary>
    /// Removes a <paramref name="prefix"/> from this path.
    /// </summary>
    /// <param name="prefix">The prefix that will be removed.</param>
    /// <param name="includeLastPart">Specifies if the last <see cref="SplitPath.Parts"/> of <paramref name="prefix"/> should be included in the new <see cref="SplitPath"/>.</param>
    /// <returns>The new created <see cref="SplitPath"/>.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public SplitPath RemovePrefix(SplitPath prefix, bool includeLastPart = false)
    {
        if (!this.StartsWith(prefix)) throw new InvalidOperationException();
        var removeLength = includeLastPart ? prefix.Parts.Length - 1 : prefix.Parts.Length;
        return new SplitPath(this.Parts[removeLength..]);
    }

    /// <summary>
    /// Determines if this path starts with <paramref name="other"/> <see cref="SplitPath"/>.
    /// </summary>
    /// <param name="other">The other <see cref="SplitPath"/> this path should start with.</param>
    /// <returns>True, if this path starts with <paramref name="other"/>, otherwise false.</returns>
    public bool StartsWith(SplitPath other) => this.Parts.Span.StartsWith(other.Parts.Span);

    public static bool operator ==(SplitPath s1, SplitPath s2)
    {
        return s1.Equals(s2);
    }

    public static bool operator !=(SplitPath s1, SplitPath s2)
    {
        return !s1.Equals(s2);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not SplitPath path) return false;
        return path.Parts.Span.SequenceEqual(this.Parts.Span);
    }
}
