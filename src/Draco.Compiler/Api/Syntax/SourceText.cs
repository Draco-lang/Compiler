using System;
using System.IO;
using Draco.Compiler.Internal.Syntax;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// Represents some source text that can be read from.
/// </summary>
public abstract class SourceText
{
    /// <summary>
    /// An empty <see cref="SourceText"/>.
    /// </summary>
    public static SourceText None { get; } = FromText(ReadOnlyMemory<char>.Empty);

    /// <summary>
    /// Wraps up the given source code as a <see cref="SourceText"/>.
    /// </summary>
    /// <param name="text">The text to wrap.</param>
    /// <returns>The constructed <see cref="SourceText"/>.</returns>
    public static SourceText FromText(string text) => FromText(text.AsMemory());

    /// <summary>
    /// Wraps up the given source code as a <see cref="SourceText"/>.
    /// </summary>
    /// <param name="text">The text to wrap.</param>
    /// <returns>The constructed <see cref="SourceText"/>.</returns>
    public static SourceText FromText(ReadOnlyMemory<char> text) => FromText(path: null, text: text);

    /// <summary>
    /// Wraps up the given source code as a <see cref="SourceText"/>.
    /// </summary>
    /// <param name="path">The path the source was read from.</param>
    /// <param name="text">The text to wrap.</param>
    /// <returns>The constructed <see cref="SourceText"/>.</returns>
    public static SourceText FromText(Uri? path, ReadOnlyMemory<char> text) =>
        new MemorySourceText(path, text);

    /// <summary>
    /// Reads a <see cref="SourceText"/> from a text file.
    /// </summary>
    /// <param name="path">The path to read from.</param>
    /// <returns>The read <see cref="SourceText"/>.</returns>
    public static SourceText FromFile(string path) => FromFile(new Uri(path));

    /// <summary>
    /// Reads a <see cref="SourceText"/> from a text file.
    /// </summary>
    /// <param name="path">The path to read from.</param>
    /// <returns>The read <see cref="SourceText"/>.</returns>
    public static SourceText FromFile(Uri path) => FromText(path: path, text: File.ReadAllText(path.LocalPath).AsMemory());

    /// <summary>
    /// The path the source originates from.
    /// </summary>
    public abstract Uri? Path { get; }

    /// <summary>
    /// Retrieves an <see cref="ISourceReader"/> for this text.
    /// </summary>
    internal abstract ISourceReader SourceReader { get; }

    /// <summary>
    /// Translates an index position into a syntax position.
    /// </summary>
    /// <param name="index">The index to translate.</param>
    /// <returns>The syntax position equivalent to <paramref name="index"/>.</returns>
    internal abstract SyntaxPosition IndexToSyntaxPosition(int index);

    /// <summary>
    /// Translates a syntax position into a 0-based index.
    /// </summary>
    /// <param name="position">The index to translate.</param>
    /// <returns>The index equivalent to <paramref name="position"/>.</returns>
    internal abstract int SyntaxPositionToIndex(SyntaxPosition position);

    /// <summary>
    /// Translatesd a source span into a syntax range.
    /// </summary>
    /// <param name="span">The source span to translate.</param>
    /// <returns>The syntax range equivalent to <paramref name="span"/>.</returns>
    internal SyntaxRange SourceSpanToSyntaxRange(SourceSpan span) => new(
        Start: this.IndexToSyntaxPosition(span.Start),
        End: this.IndexToSyntaxPosition(span.End));

    /// <summary>
    /// Translatesd a syntax range into a source span.
    /// </summary>
    /// <param name="range">The range to translate.</param>
    /// <returns>The source span equivalent to <paramref name="range"/>.</returns>
    internal SourceSpan SyntaxRangeToSourceSpan(SyntaxRange range)
    {
        var start = this.SyntaxPositionToIndex(range.Start);
        var end = this.SyntaxPositionToIndex(range.End);
        return new(Start: start, Length: end - start);
    }
}
