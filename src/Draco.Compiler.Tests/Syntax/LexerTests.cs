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

    private static string ValueText(IToken token) => ((IToken.IWithValue<string>)token).Value;

    private static void AssertNoTrivia(IToken token)
    {
        Assert.Empty(token.LeadingTrivia);
        Assert.Empty(token.TrailingTrivia);
    }

    private static void AssertNoTriviaOrDiagnostics(IToken token)
    {
        AssertNoTrivia(token);
        Assert.Empty(token.Diagnostics);
    }

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

    [Fact]
    [Trait("Feature", "Strings")]
    public void TestLineString()
    {
        var text = """
            "Hello, line strings!"
            """;
        var tokens = LexToArray(text);

        Assert.Equal(4, tokens.Length);

        Assert.Equal(TokenType.LineStringStart, tokens[0].Type);
        Assert.Equal(1, tokens[0].Width);
        AssertNoTriviaOrDiagnostics(tokens[0]);

        Assert.Equal(TokenType.StringContent, tokens[1].Type);
        Assert.Equal(20, tokens[1].Width);
        Assert.Equal("Hello, line strings!", ValueText(tokens[1]));
        AssertNoTriviaOrDiagnostics(tokens[1]);

        Assert.Equal(TokenType.LineStringEnd, tokens[2].Type);
        Assert.Equal(1, tokens[2].Width);
        AssertNoTriviaOrDiagnostics(tokens[2]);

        Assert.Equal(TokenType.EndOfInput, tokens[3].Type);
        Assert.Equal(0, tokens[3].Width);
        AssertNoTriviaOrDiagnostics(tokens[3]);
    }

    [Fact]
    [Trait("Feature", "Strings")]
    public void TestLineStringEscapes()
    {
        var text = """
            "\"\n\'\u{1F47D}"
            """;
        var tokens = LexToArray(text);

        Assert.Equal(4, tokens.Length);

        Assert.Equal(TokenType.LineStringStart, tokens[0].Type);
        Assert.Equal(1, tokens[0].Width);
        AssertNoTriviaOrDiagnostics(tokens[0]);

        Assert.Equal(TokenType.StringContent, tokens[1].Type);
        Assert.Equal(15, tokens[1].Width);
        Assert.Equal("\"\n'👽", ValueText(tokens[1]));
        AssertNoTriviaOrDiagnostics(tokens[1]);

        Assert.Equal(TokenType.LineStringEnd, tokens[2].Type);
        Assert.Equal(1, tokens[2].Width);
        AssertNoTriviaOrDiagnostics(tokens[2]);

        Assert.Equal(TokenType.EndOfInput, tokens[3].Type);
        Assert.Equal(0, tokens[3].Width);
        AssertNoTriviaOrDiagnostics(tokens[3]);
    }
}
