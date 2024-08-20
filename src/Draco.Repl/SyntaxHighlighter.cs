using System.Collections.Generic;
using Draco.Compiler.Api.Scripting;
using Draco.Compiler.Api.Syntax;

namespace Draco.Repl;

// TODO: Do we want to move this to be a compiler service?
internal static class SyntaxHighlighter
{
    public static IEnumerable<(SourceSpan Span, SyntaxColor Color)> Highlight(string text)
    {
        var tree = ReplSession.ParseReplEntry(text);

        foreach (var token in tree.Root.Tokens)
        {
            foreach (var trivia in token.LeadingTrivia)
            {
                yield return (trivia.Span, Highlight(trivia));
            }

            yield return (token.Span, Highlight(token));

            foreach (var trivia in token.TrailingTrivia)
            {
                yield return (trivia.Span, Highlight(trivia));
            }
        }
    }

    private static SyntaxColor Highlight(SyntaxToken token) => token.Kind switch
    {
        _ when SyntaxFacts.IsKeyword(token.Kind) => SyntaxColor.Keyword,
        TokenKind.LineStringStart
     or TokenKind.LineStringEnd
     or TokenKind.MultiLineStringStart
     or TokenKind.MultiLineStringEnd
     or TokenKind.StringContent => SyntaxColor.String,
        TokenKind.Identifier when token.Parent is TypeSyntax => SyntaxColor.Type,
        TokenKind.Identifier => SyntaxColor.Name,
        _ => SyntaxColor.Other,
    };

    private static SyntaxColor Highlight(SyntaxTrivia trivia) => trivia.Kind switch
    {
        TriviaKind.LineComment or TriviaKind.DocumentationComment => SyntaxColor.Comment,
        _ => SyntaxColor.Other,
    };
}
