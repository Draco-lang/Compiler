using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Debugger;

/// <summary>
/// Represents a source file from the debugged process.
/// </summary>
public sealed class SourceFile
{
    /// <summary>
    /// The document handle of this source file.
    /// </summary>
    internal DocumentHandle Document { get; }

    /// <summary>
    /// The path of the source file.
    /// </summary>
    public Uri Uri { get; }

    /// <summary>
    /// The text of the source file.
    /// </summary>
    public string Text => this.text ??= this.BuildText();
    private string? text;

    /// <summary>
    /// The lines of the source file.
    /// </summary>
    public ImmutableArray<ReadOnlyMemory<char>> Lines => this.lines ??= this.BuildLines();
    private ImmutableArray<ReadOnlyMemory<char>>? lines;

    internal SourceFile(DocumentHandle document, Uri uri)
    {
        this.Document = document;
        this.Uri = uri;
    }

    private string BuildText() => File.ReadAllText(this.Uri.LocalPath);

    private ImmutableArray<ReadOnlyMemory<char>> BuildLines()
    {
        var text = this.Text.AsMemory();
        var textSpan = text.Span;
        var result = ImmutableArray.CreateBuilder<ReadOnlyMemory<char>>();

        var prevLineStart = 0;
        for (var i = 0; i < textSpan.Length;)
        {
            var newlineLength = NewlineLength(textSpan, i);
            if (newlineLength > 0)
            {
                // This is a newline
                i += newlineLength;
                result.Add(text[prevLineStart..i]);
                prevLineStart = i;
            }
            else
            {
                // Regular character
                ++i;
            }
        }

        // Add last line
        result.Add(text[prevLineStart..textSpan.Length]);

        return result.ToImmutable();
    }

    private static int NewlineLength(ReadOnlySpan<char> str, int offset)
    {
        if (offset < 0 || offset >= str.Length) return 0;
        if (str[offset] == '\r')
        {
            // Windows or OS-X 9
            if (offset + 1 < str.Length && str[offset + 1] == '\n') return 2;
            else return 1;
        }
        if (str[offset] == '\n') return 1;
        return 0;
    }
}
