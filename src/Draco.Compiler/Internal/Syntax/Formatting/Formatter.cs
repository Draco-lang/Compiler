using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Syntax.Formatting;

/// <summary>
/// A formatter for the syntax tree.
/// </summary>
internal sealed class Formatter : SyntaxRewriter
{
    /// <summary>
    /// The settings of the formatter.
    /// </summary>
    public FormatterSettings Settings { get; }

    private readonly SyntaxTrivia newlineTrivia;

    private int indentation;
    private SyntaxToken? lastToken;

    public Formatter(FormatterSettings settings)
    {
        this.Settings = settings;

        this.newlineTrivia = SyntaxTrivia.From(TriviaKind.Newline, this.Settings.Newline);
    }

    private SyntaxToken UpdateToken(
        SyntaxToken syntaxToken,
        SyntaxList<SyntaxTrivia>.Builder leadingTrivia,
        SyntaxList<SyntaxTrivia>.Builder trailingTrivia)
    {
        var builder = SyntaxToken.Builder.From(syntaxToken);

        builder.LeadingTrivia = leadingTrivia;
        builder.TrailingTrivia = trailingTrivia;

        var result = builder.Build();
        this.lastToken = result;
        return result;
    }

    /// <summary>
    ///  * Removes all whitespace/newline.
    ///  * Adds a newline after each comment/doc comment.
    /// </summary>
    /// <param name="trivia">The trivia to normalize.</param>
    private void NormalizeTrivia(SyntaxList<SyntaxTrivia>.Builder trivia)
    {
        static bool ShouldTrim(SyntaxTrivia trivia) =>
            trivia.Kind is TriviaKind.Whitespace or TriviaKind.Newline;

        static bool IsLineComment(SyntaxTrivia trivia) =>
            trivia.Kind is TriviaKind.LineComment or TriviaKind.DocumentationComment;

        // First we just remove all whitespaces and newlines
        for (var i = 0; i < trivia.Count;)
        {
            if (ShouldTrim(trivia[i]))
            {
                trivia.RemoveAt(i);
            }
            else
            {
                ++i;
            }
        }

        // Then we add a newline after each comment/doc comment
        for (var i = 0; i < trivia.Count; ++i)
        {
            if (!IsLineComment(trivia[i])) continue;

            ++i;
            trivia.Insert(i, this.newlineTrivia);
        }
    }
}
