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
    private static IEnumerator<IToken> Lex(string text) => LexImpl(text).GetEnumerator();

    private static IEnumerable<IToken> LexImpl(string text)
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

    private static string ValueText(IToken token) => ((IToken.IWithValue<string>)token).Value;

    private static string NormalizeNewliens(string text) => text
        .Replace("\r\n", "\n")
        .Replace("\r", "\n");

    private static void AssertNextToken(IEnumerator<IToken> enumerator, out IToken result)
    {
        Assert.True(enumerator.MoveNext());
        result = enumerator.Current;
    }

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

    private static void AssertLeadingTrivia(IToken token, params string[] trivia)
    {
        Assert.Equal(trivia.Length, token.LeadingTrivia.Count);
        Assert.True(trivia.SequenceEqual(token.LeadingTrivia.Select(t => t.Text)));
    }

    private static void AssertTrailingTrivia(IToken token, params string[] trivia)
    {
        Assert.Equal(trivia.Length, token.TrailingTrivia.Count);
        Assert.True(trivia.SequenceEqual(token.TrailingTrivia.Select(t => t.Text)));
    }

    [Fact]
    [Trait("Feature", "Comments")]
    public void TestLineComment()
    {
        var text = "// Hello, comments";
        var tokens = Lex(text);

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.EndOfInput, token.Type);
        Assert.Single(token.LeadingTrivia);
        Assert.Empty(token.TrailingTrivia);
        Assert.Empty(token.Diagnostics);
        Assert.Equal(string.Empty, token.Text);
        Assert.Equal("// Hello, comments", token.LeadingTrivia[0].Text);
    }

    [Fact]
    [Trait("Feature", "Strings")]
    public void TestLineString()
    {
        var text = """
            "Hello, line strings!"
            """;
        var tokens = Lex(text);

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.LineStringStart, token.Type);
        Assert.Equal("\"", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal("Hello, line strings!", token.Text);
        Assert.Equal("Hello, line strings!", ValueText(token));
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.LineStringEnd, token.Type);
        Assert.Equal("\"", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.EndOfInput, token.Type);
        Assert.Equal(string.Empty, token.Text);
        AssertNoTriviaOrDiagnostics(token);
    }

    [Fact]
    [Trait("Feature", "Strings")]
    public void TestUnclosedLineString()
    {
        var text = """
            "Hello, line strings!
            """;
        var tokens = Lex(text);

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.LineStringStart, token.Type);
        Assert.Equal("\"", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal("Hello, line strings!", token.Text);
        Assert.Equal("Hello, line strings!", ValueText(token));
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.EndOfInput, token.Type);
        Assert.Equal(string.Empty, token.Text);
        AssertNoTriviaOrDiagnostics(token);
    }

    [Fact]
    [Trait("Feature", "Strings")]
    public void TestUnclosedLineStringWithNewlineAfter()
    {
        var text = """
        "Hello, line strings!

        """;
        var tokens = Lex(NormalizeNewliens(text));

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.LineStringStart, token.Type);
        Assert.Equal("\"", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal("Hello, line strings!", token.Text);
        Assert.Equal("Hello, line strings!", ValueText(token));
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.EndOfInput, token.Type);
        Assert.Equal(string.Empty, token.Text);
        Assert.Single(token.LeadingTrivia);
        Assert.Equal(TokenType.Newline, token.LeadingTrivia[0].Type);
        Assert.Equal("\n", token.LeadingTrivia[0].Text);
        Assert.Empty(token.TrailingTrivia);
        Assert.Empty(token.Diagnostics);
    }

    [Theory]
    [InlineData("")]
    [InlineData("#")]
    [InlineData("##")]
    [InlineData("###")]
    [Trait("Feature", "Strings")]
    public void TestLineStringEscapes(string ext)
    {
        var text = $$"""
            {{ext}}"\{{ext}}"\{{ext}}n\{{ext}}'\{{ext}}u{1F47D}"{{ext}}
            """;
        var tokens = Lex(text);

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.LineStringStart, token.Type);
        Assert.Equal($"{ext}\"", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal(@$"\{ext}""\{ext}n\{ext}'\{ext}u{{1F47D}}", token.Text);
        Assert.Equal("\"\n'👽", ValueText(token));
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.LineStringEnd, token.Type);
        Assert.Equal($"\"{ext}", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.EndOfInput, token.Type);
        Assert.Equal(string.Empty, token.Text);
        AssertNoTriviaOrDiagnostics(token);
    }

    [Theory]
    [InlineData("")]
    [InlineData("#")]
    [InlineData("##")]
    [InlineData("###")]
    [Trait("Feature", "Strings")]
    public void TestIllegalEscapeCharacterInLineString(string ext)
    {
        var text = $"""
            {ext}"\{ext}y"{ext}
            """;
        var tokens = Lex(text);

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.LineStringStart, token.Type);
        Assert.Equal($"{ext}\"", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal(@$"\{ext}y", token.Text);
        Assert.Equal("y", ValueText(token));
        AssertNoTrivia(token);
        Assert.Single(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.LineStringEnd, token.Type);
        Assert.Equal($"\"{ext}", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.EndOfInput, token.Type);
        Assert.Equal(string.Empty, token.Text);
        AssertNoTriviaOrDiagnostics(token);
    }

    [Theory]
    [InlineData("")]
    [InlineData("#")]
    [InlineData("##")]
    [InlineData("###")]
    [Trait("Feature", "Strings")]
    public void TestMultilineString(string ext)
    {
        const string quotes = "\"\"\"";
        var text = $"""
        {ext}{quotes}
            Hello!
            Bye!
            {quotes}{ext}
        """;
        var tokens = Lex(NormalizeNewliens(text));

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.MultiLineStringStart, token.Type);
        Assert.Equal($"{ext}{quotes}", token.Text);
        Assert.Empty(token.LeadingTrivia);
        Assert.Single(token.TrailingTrivia);
        Assert.Equal(TokenType.Newline, token.TrailingTrivia[0].Type);
        Assert.Empty(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal("    Hello!", token.Text);
        Assert.Equal("    Hello!", ValueText(token));
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringNewline, token.Type);
        Assert.Equal("\n", token.Text);
        Assert.Equal("\n", ValueText(token));
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal("    Bye!", token.Text);
        Assert.Equal("    Bye!", ValueText(token));
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.MultiLineStringEnd, token.Type);
        Assert.Equal($"{quotes}{ext}", token.Text);
        AssertLeadingTrivia(token, "\n", "    ");
        Assert.Empty(token.TrailingTrivia);
        Assert.Empty(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.EndOfInput, token.Type);
        Assert.Equal(string.Empty, token.Text);
        AssertNoTriviaOrDiagnostics(token);
    }

    [Theory]
    [InlineData("")]
    [InlineData("#")]
    [InlineData("##")]
    [InlineData("###")]
    [Trait("Feature", "Strings")]
    public void TestMultilineStringWithLineContinuation(string ext)
    {
        const string quotes = "\"\"\"";
        var text = $"""
        {ext}{quotes}
            Hello!\{ext}
            Bye!
            {quotes}{ext}
        """;
        var tokens = Lex(NormalizeNewliens(text));

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.MultiLineStringStart, token.Type);
        Assert.Equal($"{ext}{quotes}", token.Text);
        Assert.Empty(token.LeadingTrivia);
        Assert.Single(token.TrailingTrivia);
        Assert.Equal(TokenType.Newline, token.TrailingTrivia[0].Type);
        Assert.Empty(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal("    Hello!", token.Text);
        Assert.Equal("    Hello!", ValueText(token));
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringNewline, token.Type);
        Assert.Equal($"\\{ext}\n", token.Text);
        Assert.Equal(string.Empty, ValueText(token));
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal("    Bye!", token.Text);
        Assert.Equal("    Bye!", ValueText(token));
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.MultiLineStringEnd, token.Type);
        Assert.Equal($"{quotes}{ext}", token.Text);
        AssertLeadingTrivia(token, "\n", "    ");
        Assert.Empty(token.TrailingTrivia);
        Assert.Empty(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.EndOfInput, token.Type);
        Assert.Equal(string.Empty, token.Text);
        AssertNoTriviaOrDiagnostics(token);
    }

    [Theory]
    [InlineData("")]
    [InlineData("#")]
    [InlineData("##")]
    [InlineData("###")]
    [Trait("Feature", "Strings")]
    public void TestMultilineStringWithLineContinuationWithWhitespaceAfter(string ext)
    {
        const string quotes = "\"\"\"";
        var text = $"""
        {ext}{quotes}
            Hello!\{ext}  
            Bye!
            {quotes}{ext}
        """;
        var tokens = Lex(NormalizeNewliens(text));

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.MultiLineStringStart, token.Type);
        Assert.Equal($"{ext}{quotes}", token.Text);
        Assert.Empty(token.LeadingTrivia);
        Assert.Single(token.TrailingTrivia);
        Assert.Equal(TokenType.Newline, token.TrailingTrivia[0].Type);
        Assert.Empty(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal("    Hello!", token.Text);
        Assert.Equal("    Hello!", ValueText(token));
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringNewline, token.Type);
        Assert.Equal($"\\{ext}  \n", token.Text);
        Assert.Equal(string.Empty, ValueText(token));
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal("    Bye!", token.Text);
        Assert.Equal("    Bye!", ValueText(token));
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.MultiLineStringEnd, token.Type);
        Assert.Equal($"{quotes}{ext}", token.Text);
        AssertLeadingTrivia(token, "\n", "    ");
        Assert.Empty(token.TrailingTrivia);
        Assert.Empty(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.EndOfInput, token.Type);
        Assert.Equal(string.Empty, token.Text);
        AssertNoTriviaOrDiagnostics(token);
    }

    [Theory]
    [InlineData("")]
    [InlineData("#")]
    [InlineData("##")]
    [InlineData("###")]
    [Trait("Feature", "Strings")]
    public void TestLineStringInterpolation(string ext)
    {
        var text = $$"""
            {{ext}}"x = \{{ext}}{x}, x + y = \{{ext}}{ x + y }"{{ext}}
            """;
        var tokens = Lex(text);

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.LineStringStart, token.Type);
        Assert.Equal($"{ext}\"", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal("x = ", token.Text);
        Assert.Equal("x = ", ValueText(token));
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.InterpolationStart, token.Type);
        Assert.Equal($@"\{ext}{{", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.Identifier, token.Type);
        Assert.Equal("x", token.Text);
        Assert.Equal("x", ValueText(token));
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.InterpolationEnd, token.Type);
        Assert.Equal("}", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal(", x + y = ", token.Text);
        Assert.Equal(", x + y = ", ValueText(token));
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.InterpolationStart, token.Type);
        Assert.Equal($@"\{ext}{{", token.Text);
        Assert.Empty(token.LeadingTrivia);
        AssertTrailingTrivia(token, " ");
        Assert.Empty(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.Identifier, token.Type);
        Assert.Equal("x", token.Text);
        Assert.Equal("x", ValueText(token));
        Assert.Empty(token.LeadingTrivia);
        AssertTrailingTrivia(token, " ");
        Assert.Empty(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.Plus, token.Type);
        Assert.Equal("+", token.Text);
        Assert.Empty(token.LeadingTrivia);
        AssertTrailingTrivia(token, " ");
        Assert.Empty(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.Identifier, token.Type);
        Assert.Equal("y", token.Text);
        Assert.Equal("y", ValueText(token));
        Assert.Empty(token.LeadingTrivia);
        AssertTrailingTrivia(token, " ");
        Assert.Empty(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.InterpolationEnd, token.Type);
        Assert.Equal("}", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.LineStringEnd, token.Type);
        Assert.Equal($"\"{ext}", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.EndOfInput, token.Type);
        Assert.Equal(string.Empty, token.Text);
        AssertNoTriviaOrDiagnostics(token);
    }
}
