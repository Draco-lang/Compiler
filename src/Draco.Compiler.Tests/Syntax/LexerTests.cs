using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Syntax;

namespace Draco.Compiler.Tests.Syntax;

public sealed class LexerTests
{
    private static IEnumerable<IToken> Lex(string text)
    {
        var reader = SourceReader.From(text);
        var lexer = new Lexer(reader);
        while (true)
        {
            var token = lexer.Lex();
            yield return token;
            if (token.Type == TokenType.EndOfInput) break;
        }
    }

    private static ImmutableArray<IToken> LexToArray(string text) =>
        Lex(text).ToImmutableArray();

    [Fact]
    [Trait("Feature", "Comments")]
    public void TestLineComment()
    {
        var text = "// Hello, comments";
        var tokens = LexToArray(text);

        Assert.Single(tokens);
        Assert.Equal(TokenType.EndOfInput, tokens[0].Type);
        Assert.Single(tokens[0].LeadingTrivia);
        Assert.Empty(tokens[0].TrailingTrivia);
        Assert.Empty(tokens[0].Diagnostics);
        Assert.Equal(18, tokens[0].Width);
    }
}
