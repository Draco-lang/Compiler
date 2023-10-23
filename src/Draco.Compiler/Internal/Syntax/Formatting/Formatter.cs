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
    private readonly SyntaxTrivia whitespaceTrivia;

    private int indentation;
    private SyntaxToken? lastToken;

    public Formatter(FormatterSettings settings)
    {
        this.Settings = settings;

        this.newlineTrivia = SyntaxTrivia.From(TriviaKind.Newline, this.Settings.Newline);
        this.whitespaceTrivia = SyntaxTrivia.From(TriviaKind.Whitespace, " ");
    }

    /// <summary>
    /// Updates a token with the given leading and trailing trivia.
    /// Also sets the updated token as the <see cref="lastToken"/>.
    /// </summary>
    /// <param name="syntaxToken">The token to update.</param>
    /// <param name="leadingTrivia">The leading trivia to update with.</param>
    /// <param name="trailingTrivia">The trailing trivia to update with.</param>
    /// <returns>The updated token.</returns>
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
    /// Ensures that two trivia lists are separated by at least one whitespace.
    /// </summary>
    /// <param name="prevTrailing">The first trivia list in the sequence.</param>
    /// <param name="nextLeading">The second trivia list in the sequence.</param>
    private void EnsureWhitespaceOrNewline(
        SyntaxList<SyntaxTrivia>.Builder? prevTrailing,
        SyntaxList<SyntaxTrivia>.Builder nextLeading)
    {
        static bool IsWhitespaceOrNewline(SyntaxTrivia trivia) =>
            trivia.Kind is TriviaKind.Whitespace or TriviaKind.Newline;

        if (prevTrailing is null) return;
        if (prevTrailing.Count > 0 && IsWhitespaceOrNewline(prevTrailing[^1])) return;
        if (nextLeading.Count > 0 && IsWhitespaceOrNewline(nextLeading[0])) return;

        prevTrailing.Add(this.whitespaceTrivia);
    }

    /// <summary>
    /// Ensures that there is newlines separation between two trivia lists.
    /// </summary>
    /// <param name="prevTrailing">The first trivia list in the sequence.</param>
    /// <param name="nextLeading">The second trivia list in the sequence.</param>
    /// <param name="amount">The amount of newlines to ensure.</param>
    private void EnsureNewline(
        SyntaxList<SyntaxTrivia>.Builder? prevTrailing,
        SyntaxList<SyntaxTrivia>.Builder nextLeading,
        int amount = 1)
    {
        if (prevTrailing is null) return;

        // Count the number of newlines
        var newlineCount = 0;
        for (var i = prevTrailing.Count - 1; i >= 0; i--)
        {
            if (prevTrailing[i].Kind != TriviaKind.Newline) break;
            ++newlineCount;
        }
        for (var i = 0; i < nextLeading.Count; ++i)
        {
            if (nextLeading[i].Kind != TriviaKind.Newline) break;
            ++newlineCount;
        }

        // Add newlines if needed
        while (newlineCount < amount)
        {
            prevTrailing.Add(this.newlineTrivia);
            ++newlineCount;
        }
    }

    /// <summary>
    ///  * Removes all whitespace/newline.
    ///  * Adds a newline after each comment/doc comment.
    /// </summary>
    /// <param name="trivia">The trivia to normalize.</param>
    /// <returns>The normalized trivia.</returns>
    private SyntaxList<SyntaxTrivia>.Builder NormalizeTrivia(IEnumerable<SyntaxTrivia> trivia)
    {
        static bool ShouldTrim(SyntaxTrivia trivia) =>
            trivia.Kind is TriviaKind.Whitespace or TriviaKind.Newline;

        static bool IsLineComment(SyntaxTrivia trivia) =>
            trivia.Kind is TriviaKind.LineComment or TriviaKind.DocumentationComment;

        var result = SyntaxList.CreateBuilder<SyntaxTrivia>();
        foreach (var t in trivia)
        {
            if (ShouldTrim(t)) continue;

            result.Add(t);
            if (IsLineComment(t)) result.Add(this.newlineTrivia);
        }
        return result;
    }
}
