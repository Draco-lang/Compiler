using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Draco.Compiler.Internal;

/// <summary>
/// Represents parts of a path split by directory separator, excluding the file name.
/// </summary>
/// <param name="Parts">The path segments of this split path.</param>
internal readonly record struct SplitPath(ReadOnlyMemory<string> Parts)
{
    /// <summary>
    /// An empty path.
    /// </summary>
    public static SplitPath Empty = new(ReadOnlyMemory<string>.Empty);

    /// <summary>
    /// Creates a <see cref="SplitPath"/> from a file path, excluding the file name.
    /// </summary>
    /// <param name="path">The file path from which to create the <see cref="SplitPath"/>.</param>
    /// <returns>The created <see cref="SplitPath"/>.</returns>
    public static SplitPath FromFilePath(string path)
    {
        var splitPath = FromDirectoryPath(path);
        if (splitPath.IsEmpty) return new SplitPath(ReadOnlyMemory<string>.Empty);
        return new SplitPath(splitPath.Parts[..^1]);
    }

    /// <summary>
    /// Creates a <see cref="SplitPath"/> from a directory.
    /// </summary>
    /// <param name="path">The direcotry path from which to create <see cref="SplitPath"/>.</param>
    /// <returns>The created <see cref="SplitPath"/>.</returns>
    public static SplitPath FromDirectoryPath(string path)
    {
        var split = path.Split(
            new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);
        return new SplitPath(split.AsMemory());
    }

    /// <summary>
    /// The span of path segments.
    /// </summary>
    public ReadOnlySpan<string> Span => this.Parts.Span;

    /// <summary>
    /// The last element of the path.
    /// </summary>
    public string Last => this.Span[^1];

    /// <summary>
    /// True, if this path contains no segments.
    /// </summary>
    public bool IsEmpty => this.Length == 0;

    /// <summary>
    /// The number of sections in this path.
    /// </summary>
    public int Length => this.Parts.Length;

    /// <summary>
    /// Determines if this path starts with <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The other <see cref="SplitPath"/> this path should start with.</param>
    /// <returns>True, if this path starts with <paramref name="other"/>, otherwise false.</returns>
    public bool StartsWith(SplitPath other) => this.Span.StartsWith(other.Span);

    /// <summary>
    /// Removes a <paramref name="prefix"/> from this path.
    /// </summary>
    /// <param name="prefix">The prefix that will be removed.</param>
    /// <returns>A new <see cref="SplitPath"/> with <paramref name="prefix"/> removed.</returns>
    public SplitPath RemovePrefix(SplitPath prefix)
    {
        if (!this.StartsWith(prefix))
        {
            throw new ArgumentException("the split path does not start with the given prefix", nameof(prefix));
        }
        return this.Slice(prefix.Length..);
    }

    /// <summary>
    /// Slices this path given a range.
    /// </summary>
    /// <param name="range">The range to slice by.</param>
    /// <returns>The sub-path of this path sliced by <paramref name="range"/>.</returns>
    public SplitPath Slice(Range range) => new(this.Parts[range]);

    public SplitPath Append(params string[] text) =>
        new SplitPath(this.Parts.ToArray().Concat(text).ToArray().AsMemory());

    public bool Equals(SplitPath other) =>
        this.Span.SequenceEqual(other.Span);

    public override int GetHashCode()
    {
        var h = default(HashCode);
        foreach (var part in this.Span) h.Add(part);
        return h.ToHashCode();
    }

    public override string ToString() => string.Join(".", MemoryMarshal.ToEnumerable(this.Parts));
}
