using System;
using System.Collections.Generic;
using System.Threading;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// An in-memory <see cref="Api.Syntax.SourceText"/> implementation.
/// </summary>
internal sealed class MemorySourceText(Uri? path, ReadOnlyMemory<char> content) : Api.Syntax.SourceText
{
    public override Uri? Path { get; } = path;
    internal override ISourceReader SourceReader => Syntax.SourceReader.From(content);

    private List<int>? lineStarts;

    internal override Api.Syntax.SyntaxPosition IndexToSyntaxPosition(int index)
    {
        var lineStarts = LazyInitializer.EnsureInitialized(ref this.lineStarts, this.BuildLineStarts);
        var lineIndex = lineStarts.BinarySearch(index);
        // No exact match, we need the previous line
        if (lineIndex < 0) lineIndex = ~lineIndex - 1;
        // From this, the position is simply the line index for the line
        // and the index - the line start index for the column
        return new(Line: lineIndex, Column: index - lineStarts[lineIndex]);
    }

    internal override int SyntaxPositionToIndex(Api.Syntax.SyntaxPosition position)
    {
        var lineStarts = LazyInitializer.EnsureInitialized(ref this.lineStarts, this.BuildLineStarts);

        // Avoid over-indexing
        if (position.Line >= lineStarts.Count) return content.Length;

        var lineOffset = lineStarts[position.Line];
        var nextLineOffset = position.Line + 1 >= lineStarts.Count
            ? content.Length
            : lineStarts[position.Line + 1];
        var lineLength = nextLineOffset - lineOffset;
        var columnOffset = Math.Min(lineLength, position.Column);

        return lineOffset + columnOffset;
    }

    private List<int> BuildLineStarts()
    {
        var result = new List<int>
        {
            // First line
            0
        };

        for (var i = 0; i < content.Length;)
        {
            var newlineLength = StringUtils.NewlineLength(content.Span, i);
            if (newlineLength > 0)
            {
                // This is a newline, add the next line start
                i += newlineLength;
                result.Add(i);
            }
            else
            {
                // Regular character
                ++i;
            }
        }

        return result;
    }
}
