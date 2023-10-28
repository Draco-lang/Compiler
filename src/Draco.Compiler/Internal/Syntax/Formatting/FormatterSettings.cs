using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Syntax.Formatting;

/// <summary>
/// The settings of the formatter.
/// </summary>
internal sealed class FormatterSettings
{
    /// <summary>
    /// The default formatting settings.
    /// </summary>
    public static FormatterSettings Default { get; } = new();

    /// <summary>
    /// The newline sequence.
    /// </summary>
    public string Newline { get; init; } = "\n";

    /// <summary>
    /// The indentation sequence.
    /// </summary>
    public string Indentation { get; init; } = "    ";

    /// <summary>
    /// True, if newlines in strings should be normalized to the <see cref="Newline"/> sequence.
    /// </summary>
    public bool NormalizeStringNewline { get; init; } = true;

    /// <summary>
    /// The minimum number of consecutive variable declarations to align them by type and value.
    /// Negative numbers disable this feature.
    /// </summary>
    public int VariablesAlignmentGroupMinSize { get; init; } = -1;

    public string IndentationString(int amount = 1)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < amount; ++i) sb.Append(this.Indentation);
        return sb.ToString();
    }
    public string PaddingString(int width = 1)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < width; ++i) sb.Append(' ');
        return sb.ToString();
    }

    public SyntaxTrivia NewlineTrivia => new(Api.Syntax.TriviaKind.Newline, this.Newline);
    public SyntaxTrivia SpaceTrivia => new(Api.Syntax.TriviaKind.Whitespace, " ");
    public SyntaxTrivia IndentationTrivia(int amount = 1) =>
        new(Api.Syntax.TriviaKind.Whitespace, this.IndentationString(amount));
    public SyntaxTrivia PaddingTrivia(int width = 1) =>
        new(Api.Syntax.TriviaKind.Whitespace, this.PaddingString(width));
}
