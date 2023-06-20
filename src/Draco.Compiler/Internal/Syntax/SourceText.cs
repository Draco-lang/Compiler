using System;
using System.Collections.Generic;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// An in-memory <see cref="Api.Syntax.SourceText"/> implementation.
/// </summary>
internal sealed class MemorySourceText : Api.Syntax.SourceText
{
    public override Uri? Path { get; }
    internal override ISourceReader SourceReader => Syntax.SourceReader.From(this.content);

    private readonly ReadOnlyMemory<char> content;
    private List<int>? lineStarts;

    public MemorySourceText(Uri? path, ReadOnlyMemory<char> content)
    {
        this.Path = path;
        this.content = content;
    }

    internal override Api.Syntax.SyntaxPosition IndexToSyntaxPosition(int index)
    {
        var lineStarts = InterlockedUtils.InitializeNull(ref this.lineStarts, this.BuildLineStarts);
        var lineIndex = lineStarts.BinarySearch(index);
        // No exact match, we need the previous line
        if (lineIndex < 0) lineIndex = ~lineIndex - 1;
        // From this, the position is simply the line index for the line
        // and the index - the line start index for the column
        return new(Line: lineIndex, Column: index - lineStarts[lineIndex]);
    }

    internal override int SyntaxPositionToIndex(Api.Syntax.SyntaxPosition position)
    {
        var lineStarts = InterlockedUtils.InitializeNull(ref this.lineStarts, this.BuildLineStarts);

        // Avoid over-indexing
        if (position.Line >= lineStarts.Count) return this.content.Length;

        var lineOffset = lineStarts[position.Line];
        var nextLineOffset = position.Line + 1 >= lineStarts.Count
            ? this.content.Length
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

        for (var i = 0; i < this.content.Length;)
        {
            var newlineLength = StringUtils.NewlineLength(this.content.Span, i);
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
