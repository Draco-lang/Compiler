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

    private static string NormalizeNewliens(string text) => text
        .Replace("\r\n", "\n")
        .Replace("\r", "\n");

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
        Assert.Equal(string.Empty, tokens[0].Text);
        Assert.Equal("// Hello, comments", tokens[0].LeadingTrivia[0].Text);
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
        Assert.Equal("\"", tokens[0].Text);
        AssertNoTriviaOrDiagnostics(tokens[0]);

        Assert.Equal(TokenType.StringContent, tokens[1].Type);
        Assert.Equal("Hello, line strings!", tokens[1].Text);
        Assert.Equal("Hello, line strings!", ValueText(tokens[1]));
        AssertNoTriviaOrDiagnostics(tokens[1]);

        Assert.Equal(TokenType.LineStringEnd, tokens[2].Type);
        Assert.Equal("\"", tokens[2].Text);
        AssertNoTriviaOrDiagnostics(tokens[2]);

        Assert.Equal(TokenType.EndOfInput, tokens[3].Type);
        Assert.Equal(string.Empty, tokens[3].Text);
        AssertNoTriviaOrDiagnostics(tokens[3]);
    }

    [Fact]
    [Trait("Feature", "Strings")]
    public void TestUnclosedLineString()
    {
        var text = """
            "Hello, line strings!
            """;
        var tokens = LexToArray(text);

        Assert.Equal(3, tokens.Length);

        Assert.Equal(TokenType.LineStringStart, tokens[0].Type);
        Assert.Equal("\"", tokens[0].Text);
        AssertNoTriviaOrDiagnostics(tokens[0]);

        Assert.Equal(TokenType.StringContent, tokens[1].Type);
        Assert.Equal("Hello, line strings!", tokens[1].Text);
        Assert.Equal("Hello, line strings!", ValueText(tokens[1]));
        AssertNoTriviaOrDiagnostics(tokens[1]);

        Assert.Equal(TokenType.EndOfInput, tokens[2].Type);
        Assert.Equal(string.Empty, tokens[2].Text);
        AssertNoTriviaOrDiagnostics(tokens[2]);
    }

    [Fact]
    [Trait("Feature", "Strings")]
    public void TestUnclosedLineStringWithNewlineAfter()
    {
        var text = """
        "Hello, line strings!

        """;
        var tokens = LexToArray(NormalizeNewliens(text));

        Assert.Equal(3, tokens.Length);

        Assert.Equal(TokenType.LineStringStart, tokens[0].Type);
        Assert.Equal("\"", tokens[0].Text);
        AssertNoTriviaOrDiagnostics(tokens[0]);

        Assert.Equal(TokenType.StringContent, tokens[1].Type);
        Assert.Equal("Hello, line strings!", tokens[1].Text);
        Assert.Equal("Hello, line strings!", ValueText(tokens[1]));
        AssertNoTriviaOrDiagnostics(tokens[1]);

        Assert.Equal(TokenType.EndOfInput, tokens[2].Type);
        Assert.Equal(string.Empty, tokens[2].Text);
        Assert.Single(tokens[2].LeadingTrivia);
        Assert.Equal(TokenType.Newline, tokens[2].LeadingTrivia[0].Type);
        Assert.Equal("\n", tokens[2].LeadingTrivia[0].Text);
        Assert.Empty(tokens[2].TrailingTrivia);
        Assert.Empty(tokens[2].Diagnostics);
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
        Assert.Equal("\"", tokens[0].Text);
        AssertNoTriviaOrDiagnostics(tokens[0]);

        Assert.Equal(TokenType.StringContent, tokens[1].Type);
        Assert.Equal(@"\""\n\'\u{1F47D}", tokens[1].Text);
        Assert.Equal("\"\n'👽", ValueText(tokens[1]));
        AssertNoTriviaOrDiagnostics(tokens[1]);

        Assert.Equal(TokenType.LineStringEnd, tokens[2].Type);
        Assert.Equal("\"", tokens[2].Text);
        AssertNoTriviaOrDiagnostics(tokens[2]);

        Assert.Equal(TokenType.EndOfInput, tokens[3].Type);
        Assert.Equal(string.Empty, tokens[3].Text);
        AssertNoTriviaOrDiagnostics(tokens[3]);
    }

    [Fact]
    [Trait("Feature", "Strings")]
    public void TestIllegalEscapeCharacterInLineString()
    {
        var text = """
            "\y"
            """;
        var tokens = LexToArray(text);

        Assert.Equal(4, tokens.Length);

        Assert.Equal(TokenType.LineStringStart, tokens[0].Type);
        Assert.Equal("\"", tokens[0].Text);
        AssertNoTriviaOrDiagnostics(tokens[0]);

        Assert.Equal(TokenType.StringContent, tokens[1].Type);
        Assert.Equal(@"\y", tokens[1].Text);
        Assert.Equal("y", ValueText(tokens[1]));
        AssertNoTrivia(tokens[1]);
        Assert.Single(tokens[1].Diagnostics);

        Assert.Equal(TokenType.LineStringEnd, tokens[2].Type);
        Assert.Equal("\"", tokens[2].Text);
        AssertNoTriviaOrDiagnostics(tokens[2]);

        Assert.Equal(TokenType.EndOfInput, tokens[3].Type);
        Assert.Equal(string.Empty, tokens[3].Text);
        AssertNoTriviaOrDiagnostics(tokens[3]);
    }

    [Fact]
    [Trait("Feature", "Strings")]
    public void TestMultilineString()
    {
        const string quotes = "\"\"\"";
        var text = $"""
        {quotes}
            Hello!
            Bye!
            {quotes}
        """;
        var tokens = LexToArray(NormalizeNewliens(text));

        Assert.Equal(5, tokens.Length);

        Assert.Equal(TokenType.MultiLineStringStart, tokens[0].Type);
        Assert.Equal(quotes, tokens[0].Text);
        Assert.Empty(tokens[0].LeadingTrivia);
        Assert.Single(tokens[0].TrailingTrivia);
        Assert.Equal(TokenType.Newline, tokens[0].TrailingTrivia[0].Type);
        Assert.Empty(tokens[0].Diagnostics);

        Assert.Equal(TokenType.StringContent, tokens[1].Type);
        Assert.Equal("    Hello!", tokens[1].Text);
        Assert.Equal("    Hello!", ValueText(tokens[1]));
        AssertNoTriviaOrDiagnostics(tokens[1]);

        Assert.Equal(TokenType.StringNewline, tokens[2].Type);
        Assert.Equal("\n", tokens[2].Text);
        Assert.Equal("\n", ValueText(tokens[2]));
        AssertNoTriviaOrDiagnostics(tokens[2]);

        Assert.Equal(TokenType.StringContent, tokens[2].Type);
        Assert.Equal("    Bye!", tokens[2].Text);
        Assert.Equal("    Bye!", ValueText(tokens[2]));
        AssertNoTriviaOrDiagnostics(tokens[2]);

        Assert.Equal(TokenType.MultiLineStringEnd, tokens[3].Type);
        Assert.Equal(quotes, tokens[3].Text);
        Assert.Equal(2, tokens[3].LeadingTrivia.Count);
        Assert.Equal("\n", tokens[3].LeadingTrivia[0].Text);
        Assert.Equal("    ", tokens[3].LeadingTrivia[1].Text);
        Assert.Empty(tokens[3].TrailingTrivia);
        Assert.Empty(tokens[3].Diagnostics);

        Assert.Equal(TokenType.EndOfInput, tokens[4].Type);
        Assert.Equal(string.Empty, tokens[4].Text);
        AssertNoTriviaOrDiagnostics(tokens[4]);
    }

    [Fact]
    [Trait("Feature", "Strings")]
    public void TestMultilineStringWithLineContinuation()
    {
        const string quotes = "\"\"\"";
        var text = $"""
        {quotes}
            Hello!\
            Bye!
            {quotes}
        """;
        var tokens = LexToArray(NormalizeNewliens(text));

        Assert.Equal(5, tokens.Length);

        Assert.Equal(TokenType.MultiLineStringStart, tokens[0].Type);
        Assert.Equal(quotes, tokens[0].Text);
        Assert.Empty(tokens[0].LeadingTrivia);
        Assert.Single(tokens[0].TrailingTrivia);
        Assert.Equal(TokenType.Newline, tokens[0].TrailingTrivia[0].Type);
        Assert.Empty(tokens[0].Diagnostics);

        Assert.Equal(TokenType.StringContent, tokens[1].Type);
        Assert.Equal("    Hello!\\", tokens[1].Text);
        Assert.Equal("    Hello!", ValueText(tokens[1]));
        AssertNoTriviaOrDiagnostics(tokens[1]);

        Assert.Equal(TokenType.StringNewline, tokens[2].Type);
        Assert.Equal("\n", tokens[2].Text);
        Assert.Equal(string.Empty, ValueText(tokens[2]));
        AssertNoTriviaOrDiagnostics(tokens[2]);

        Assert.Equal(TokenType.StringContent, tokens[2].Type);
        Assert.Equal("    Bye!", tokens[2].Text);
        Assert.Equal("    Bye!", ValueText(tokens[2]));
        AssertNoTriviaOrDiagnostics(tokens[2]);

        Assert.Equal(TokenType.MultiLineStringEnd, tokens[3].Type);
        Assert.Equal(quotes, tokens[3].Text);
        Assert.Equal(2, tokens[3].LeadingTrivia.Count);
        Assert.Equal("\n", tokens[3].LeadingTrivia[0].Text);
        Assert.Equal("    ", tokens[3].LeadingTrivia[1].Text);
        Assert.Empty(tokens[3].TrailingTrivia);
        Assert.Empty(tokens[3].Diagnostics);

        Assert.Equal(TokenType.EndOfInput, tokens[4].Type);
        Assert.Equal(string.Empty, tokens[4].Text);
        AssertNoTriviaOrDiagnostics(tokens[4]);
    }
}
