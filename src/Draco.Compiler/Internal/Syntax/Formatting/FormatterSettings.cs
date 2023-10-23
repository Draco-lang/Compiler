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

    public SyntaxTrivia NewlineTrivia => new(Api.Syntax.TriviaKind.Newline, this.Newline);
    public SyntaxTrivia SpaceTrivia => new(Api.Syntax.TriviaKind.Whitespace, " ");
    public SyntaxTrivia IndentationTrivia(int amount = 1)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < amount; ++i) sb.Append(this.Indentation);
        return new(Api.Syntax.TriviaKind.Whitespace, sb.ToString());
    }
}
