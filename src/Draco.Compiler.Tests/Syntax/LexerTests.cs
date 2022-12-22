using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Syntax;
using static Draco.Compiler.Internal.Syntax.ParseNode;

namespace Draco.Compiler.Tests.Syntax;

public sealed class LexerTests
{
    private static IEnumerator<Token> Lex(string text) => LexImpl(text).GetEnumerator();

    private static IEnumerable<Token> LexImpl(string text)
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

    private static string NormalizeNewliens(string text) => text
        .Replace("\r\n", "\n")
        .Replace("\r", "\n");

    private static void AssertNextToken(IEnumerator<Token> enumerator, out Token result)
    {
        Assert.True(enumerator.MoveNext());
        result = enumerator.Current;
    }

    private static void AssertNoTrivia(Token token)
    {
        Assert.Empty(token.LeadingTrivia);
        Assert.Empty(token.TrailingTrivia);
    }

    private static void AssertNoTriviaOrDiagnostics(Token token)
    {
        AssertNoTrivia(token);
        Assert.Empty(token.Diagnostics);
    }

    private static void AssertLeadingTrivia(Token token, params string[] trivia)
    {
        Assert.Equal(trivia.Length, token.LeadingTrivia.Length);
        Assert.True(trivia.SequenceEqual(token.LeadingTrivia.Select(t => t.Text)));
    }

    private static void AssertTrailingTrivia(Token token, params string[] trivia)
    {
        Assert.Equal(trivia.Length, token.TrailingTrivia.Length);
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
        Assert.Equal("Hello, line strings!", token.ValueText);
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
        Assert.Equal("Hello, line strings!", token.ValueText);
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
        Assert.Equal("Hello, line strings!", token.ValueText);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.EndOfInput, token.Type);
        Assert.Equal(string.Empty, token.Text);
        Assert.Single(token.LeadingTrivia);
        Assert.Equal(TriviaType.Newline, token.LeadingTrivia[0].Type);
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
            {{ext}}"\{{ext}}"\{{ext}}\\{{ext}}n\{{ext}}'\{{ext}}u{1F47D}\{{ext}}0"{{ext}}
            """;
        var tokens = Lex(text);

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.LineStringStart, token.Type);
        Assert.Equal($"{ext}\"", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal(@$"\{ext}""\{ext}\\{ext}n\{ext}'\{ext}u{{1F47D}}\{ext}0", token.Text);
        Assert.Equal("\"\\\n'👽\0", token.ValueText);
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
    public void TestLineStringZeroLengthUnicodeCodepoint(string ext)
    {
        var text = $$"""
            {{ext}}"\{{ext}}u{}"{{ext}}
            """;
        var tokens = Lex(text);

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.LineStringStart, token.Type);
        Assert.Equal($"{ext}\"", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal(@$"\{ext}u{{}}", token.Text);
        Assert.Equal("", token.ValueText);
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
    public void TestLineStringInvalidUnicodeCodepoint(string ext)
    {
        var text = $$"""
            {{ext}}"\{{ext}}u{3S}"{{ext}}
            """;
        var tokens = Lex(text);

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.LineStringStart, token.Type);
        Assert.Equal($"{ext}\"", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal(@$"\{ext}u{{3S}}", token.Text);
        Assert.Equal("S}", token.ValueText); //TODO: change this when we get better orrors out of invalid unicode codepoints
        AssertNoTrivia(token);
        Assert.Single(token.Diagnostics);
        Assert.Equal("unclosed unicode codepoint escape sequence", token.Diagnostics[0].Format);

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
    public void TestLineStringUnclosedUnicodeCodepoint(string ext)
    {
        var text = $$"""
            {{ext}}"\{{ext}}u{"{{ext}}
            """;
        var tokens = Lex(text);

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.LineStringStart, token.Type);
        Assert.Equal($"{ext}\"", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal(@$"\{ext}u{{", token.Text);
        Assert.Equal("", token.ValueText);
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

    [Fact]
    [Trait("Feature", "Strings")]
    public void TestLineStringMixedEscapes()
    {
        var text = $$"""
            ##"\a\#n\#u{123}\##t"##
            """;
        var tokens = Lex(text);

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.LineStringStart, token.Type);
        Assert.Equal("##\"", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal(@"\a\#n\#u{123}\##t", token.Text);
        Assert.Equal("\\a\\#n\\#u{123}\t", token.ValueText);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.LineStringEnd, token.Type);
        Assert.Equal($"\"##", token.Text);
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
        Assert.Equal("y", token.ValueText);
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
        var text = $""""
        {ext}"""
            Hello!
            Bye!
            """{ext}
        """";
        var tokens = Lex(NormalizeNewliens(text));

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.MultiLineStringStart, token.Type);
        Assert.Equal($"{ext}{quotes}", token.Text);
        Assert.Empty(token.LeadingTrivia);
        Assert.Single(token.TrailingTrivia);
        Assert.Equal(TriviaType.Newline, token.TrailingTrivia[0].Type);
        Assert.Empty(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal("    Hello!", token.Text);
        Assert.Equal("    Hello!", token.ValueText);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringNewline, token.Type);
        Assert.Equal("\n", token.Text);
        Assert.Equal("\n", token.ValueText);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal("    Bye!", token.Text);
        Assert.Equal("    Bye!", token.ValueText);
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
    public void TestEmptyMultilineString(string ext)
    {
        const string quotes = "\"\"\"";
        var text = $""""
        {ext}"""
        """{ext}
        """";
        var tokens = Lex(NormalizeNewliens(text));

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.MultiLineStringStart, token.Type);
        Assert.Equal($"{ext}{quotes}", token.Text);
        Assert.Empty(token.LeadingTrivia);
        AssertTrailingTrivia(token, "\n");
        Assert.Equal(TriviaType.Newline, token.TrailingTrivia[0].Type);
        Assert.Empty(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.MultiLineStringEnd, token.Type);
        Assert.Equal($"{quotes}{ext}", token.Text);
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
    public void TestEmptyMultilineStringWithIndentedClosingQuotes(string ext)
    {
        const string quotes = "\"\"\"";
        var text = $""""
        {ext}"""
            """{ext}
        """";
        var tokens = Lex(NormalizeNewliens(text));

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.MultiLineStringStart, token.Type);
        Assert.Equal($"{ext}{quotes}", token.Text);
        Assert.Empty(token.LeadingTrivia);
        AssertTrailingTrivia(token, "\n");
        Assert.Equal(TriviaType.Newline, token.TrailingTrivia[0].Type);
        Assert.Empty(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.MultiLineStringEnd, token.Type);
        Assert.Equal($"{quotes}{ext}", token.Text);
        AssertLeadingTrivia(token, "    ");
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
    public void TestEmptyMultilineStringWithInlineClosingQuotes(string ext)
    {
        const string quotes = "\"\"\"";
        var text = $""""
        {ext}"""    """{ext}
        """";
        var tokens = Lex(NormalizeNewliens(text));

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.MultiLineStringStart, token.Type);
        Assert.Equal($"{ext}{quotes}", token.Text);
        Assert.Empty(token.LeadingTrivia);
        AssertTrailingTrivia(token, "    ");
        Assert.Empty(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.MultiLineStringEnd, token.Type);
        Assert.Equal($"{quotes}{ext}", token.Text);
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
    public void TestMultilineStringWithInlineClosingQuotes(string ext)
    {
        const string quotes = "\"\"\"";
        var text = $""""
        {ext}""" hello """{ext}
        """";
        var tokens = Lex(NormalizeNewliens(text));

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.MultiLineStringStart, token.Type);
        Assert.Equal($"{ext}{quotes}", token.Text);
        Assert.Empty(token.LeadingTrivia);
        AssertTrailingTrivia(token, " ");
        Assert.Empty(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal($"hello", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.MultiLineStringEnd, token.Type);
        Assert.Equal($"{quotes}{ext}", token.Text);
        AssertLeadingTrivia(token, " ");
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
    public void TestMultilineStringWithUnindentedClosingQuotes(string ext)
    {
        const string quotes = "\"\"\"";
        var text = $""""
        {ext}"""
            Hello!
            Bye!
        """{ext}
        """";
        var tokens = Lex(NormalizeNewliens(text));

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.MultiLineStringStart, token.Type);
        Assert.Equal($"{ext}{quotes}", token.Text);
        Assert.Empty(token.LeadingTrivia);
        Assert.Single(token.TrailingTrivia);
        Assert.Equal(TriviaType.Newline, token.TrailingTrivia[0].Type);
        Assert.Empty(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal("    Hello!", token.Text);
        Assert.Equal("    Hello!", token.ValueText);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringNewline, token.Type);
        Assert.Equal("\n", token.Text);
        Assert.Equal("\n", token.ValueText);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal("    Bye!", token.Text);
        Assert.Equal("    Bye!", token.ValueText);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.MultiLineStringEnd, token.Type);
        Assert.Equal($"{quotes}{ext}", token.Text);
        AssertLeadingTrivia(token, "\n");
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
        var text = $""""
        {ext}"""
            Hello!\{ext}
            Bye!
            """{ext}
        """";
        var tokens = Lex(NormalizeNewliens(text));

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.MultiLineStringStart, token.Type);
        Assert.Equal($"{ext}{quotes}", token.Text);
        Assert.Empty(token.LeadingTrivia);
        Assert.Single(token.TrailingTrivia);
        Assert.Equal(TriviaType.Newline, token.TrailingTrivia[0].Type);
        Assert.Empty(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal("    Hello!", token.Text);
        Assert.Equal("    Hello!", token.ValueText);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringNewline, token.Type);
        Assert.Equal($"\\{ext}\n", token.Text);
        Assert.Equal(string.Empty, token.ValueText);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal("    Bye!", token.Text);
        Assert.Equal("    Bye!", token.ValueText);
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
        const string trailingSpace = "  ";
        const string quotes = "\"\"\"";
        var text = $""""
        {ext}"""
            Hello!\{ext}{trailingSpace}
            Bye!
            """{ext}
        """";
        var tokens = Lex(NormalizeNewliens(text));

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.MultiLineStringStart, token.Type);
        Assert.Equal($"{ext}{quotes}", token.Text);
        Assert.Empty(token.LeadingTrivia);
        Assert.Single(token.TrailingTrivia);
        Assert.Equal(TriviaType.Newline, token.TrailingTrivia[0].Type);
        Assert.Empty(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal("    Hello!", token.Text);
        Assert.Equal("    Hello!", token.ValueText);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringNewline, token.Type);
        Assert.Equal($"\\{ext}{trailingSpace}\n", token.Text);
        Assert.Equal(string.Empty, token.ValueText);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal("    Bye!", token.Text);
        Assert.Equal("    Bye!", token.ValueText);
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
            {{ext}}"x = \{{ext}}{x}, x + y = \{{ext}}{ x + y }, y = \{{ext}}{ {y} }"{{ext}}
            """;
        var tokens = Lex(text);

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.LineStringStart, token.Type);
        Assert.Equal($"{ext}\"", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal("x = ", token.Text);
        Assert.Equal("x = ", token.ValueText);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.InterpolationStart, token.Type);
        Assert.Equal($@"\{ext}{{", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.Identifier, token.Type);
        Assert.Equal("x", token.Text);
        Assert.Equal("x", token.ValueText);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.InterpolationEnd, token.Type);
        Assert.Equal("}", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal(", x + y = ", token.Text);
        Assert.Equal(", x + y = ", token.ValueText);
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
        Assert.Equal("x", token.ValueText);
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
        Assert.Equal("y", token.ValueText);
        Assert.Empty(token.LeadingTrivia);
        AssertTrailingTrivia(token, " ");
        Assert.Empty(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.InterpolationEnd, token.Type);
        Assert.Equal("}", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal(", y = ", token.Text);
        Assert.Equal(", y = ", token.ValueText);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.InterpolationStart, token.Type);
        Assert.Equal($@"\{ext}{{", token.Text);
        Assert.Empty(token.LeadingTrivia);
        AssertTrailingTrivia(token, " ");
        Assert.Empty(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.CurlyOpen, token.Type);
        Assert.Equal("{", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.Identifier, token.Type);
        Assert.Equal("y", token.Text);
        Assert.Equal("y", token.ValueText);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.CurlyClose, token.Type);
        Assert.Equal("}", token.Text);
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

    [Fact]
    [Trait("Feature", "Strings")]
    public void TestNewlineInLineStringInterpolation()
    {
        // "hello\{
        // var\}bye"
        var text = "\"hello\\{\nvar}bye\"";
        var tokens = Lex(text);

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.LineStringStart, token.Type);
        Assert.Equal("\"", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal("hello", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.InterpolationStart, token.Type);
        Assert.Equal(@"\{", token.Text);
        Assert.Empty(token.LeadingTrivia);
        AssertTrailingTrivia(token, "\n");
        Assert.Empty(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.KeywordVar, token.Type);
        Assert.Equal("var", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.CurlyClose, token.Type);
        Assert.Equal("}", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.Identifier, token.Type);
        Assert.Equal("bye", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.LineStringStart, token.Type);
        Assert.Equal("\"", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.EndOfInput, token.Type);
        Assert.Equal(string.Empty, token.Text);
        AssertNoTriviaOrDiagnostics(token);
    }

    [Fact]
    [Trait("Feature", "Strings")]
    public void TestNewlineInLineStringInterpolationNestedString()
    {
        // "hello\{"bye
        // var\}baz"
        var text = "\"hello\\{\"bye\nvar}baz\"";
        var tokens = Lex(text);

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.LineStringStart, token.Type);
        Assert.Equal("\"", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal("hello", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.InterpolationStart, token.Type);
        Assert.Equal(@"\{", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.LineStringStart, token.Type);
        Assert.Equal("\"", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal("bye", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.KeywordVar, token.Type);
        Assert.Equal("var", token.Text);
        Assert.Single(token.LeadingTrivia);
        Assert.Equal("\n", token.LeadingTrivia[0].Text);
        Assert.Empty(token.TrailingTrivia);
        Assert.Empty(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.CurlyClose, token.Type);
        Assert.Equal("}", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.Identifier, token.Type);
        Assert.Equal("baz", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.LineStringStart, token.Type);
        Assert.Equal("\"", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.EndOfInput, token.Type);
        Assert.Equal(string.Empty, token.Text);
        AssertNoTriviaOrDiagnostics(token);
    }

    [Fact]
    [Trait("Feature", "Strings")]
    public void TestNewlineInMultilineStringInterpolation()
    {
        // """
        // foo\{
        // x}bar
        // """
        var quotes = "\"\"\"";
        var text = $"{quotes}\nfoo\\{{\nx}}bar\n{quotes}";
        var tokens = Lex(text);

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.MultiLineStringStart, token.Type);
        Assert.Equal(quotes, token.Text);
        Assert.Empty(token.LeadingTrivia);
        Assert.Single(token.TrailingTrivia);
        Assert.Equal("\n", token.TrailingTrivia[0].Text);
        Assert.Empty(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal("foo", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.InterpolationStart, token.Type);
        Assert.Equal(@"\{", token.Text);
        Assert.Empty(token.LeadingTrivia);
        Assert.Single(token.TrailingTrivia);
        Assert.Equal("\n", token.TrailingTrivia[0].Text);
        Assert.Empty(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.Identifier, token.Type);
        Assert.Equal("x", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.InterpolationEnd, token.Type);
        Assert.Equal("}", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal("bar", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.MultiLineStringEnd, token.Type);
        Assert.Equal(quotes, token.Text);
        Assert.Single(token.LeadingTrivia);
        Assert.Equal("\n", token.LeadingTrivia[0].Text);
        Assert.Empty(token.TrailingTrivia);
        Assert.Empty(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.EndOfInput, token.Type);
        Assert.Equal(string.Empty, token.Text);
        AssertNoTriviaOrDiagnostics(token);
    }

    [Fact]
    [Trait("Feature", "Strings")]
    public void TestNewlineInMultilineStringInterpolationNestedString()
    {
        // """
        // foo\{"bar
        // x}baz
        // """
        var quotes = "\"\"\"";
        var text = $"{quotes}\nfoo\\{{\"bar\nx}}baz\n{quotes}";
        var tokens = Lex(text);

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.MultiLineStringStart, token.Type);
        Assert.Equal(quotes, token.Text);
        Assert.Empty(token.LeadingTrivia);
        Assert.Single(token.TrailingTrivia);
        Assert.Equal("\n", token.TrailingTrivia[0].Text);
        Assert.Empty(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal("foo", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.InterpolationStart, token.Type);
        Assert.Equal(@"\{", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.LineStringStart, token.Type);
        Assert.Equal("\"", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal("bar", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.Identifier, token.Type);
        Assert.Equal("x", token.Text);
        Assert.Single(token.LeadingTrivia);
        Assert.Equal("\n", token.LeadingTrivia[0].Text);
        Assert.Empty(token.TrailingTrivia);
        Assert.Empty(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.InterpolationEnd, token.Type);
        Assert.Equal("}", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal("baz", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.MultiLineStringEnd, token.Type);
        Assert.Equal(quotes, token.Text);
        Assert.Single(token.LeadingTrivia);
        Assert.Equal("\n", token.LeadingTrivia[0].Text);
        Assert.Empty(token.TrailingTrivia);
        Assert.Empty(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.EndOfInput, token.Type);
        Assert.Equal(string.Empty, token.Text);
        AssertNoTriviaOrDiagnostics(token);
    }

    [Theory]
    [InlineData("if", TokenType.KeywordIf)]
    [InlineData("else", TokenType.KeywordElse)]
    [InlineData("while", TokenType.KeywordWhile)]
    [InlineData("if_", TokenType.Identifier)]
    [InlineData("ifa", TokenType.Identifier)]
    [InlineData("if0", TokenType.Identifier)]
    [InlineData("_if", TokenType.Identifier)]
    [InlineData("hello", TokenType.Identifier)]
    [InlineData("hello123", TokenType.Identifier)]
    [InlineData("hello_123", TokenType.Identifier)]
    [InlineData("_hello_123", TokenType.Identifier)]
    [Trait("Feature", "Words")]
    public void TestKeyword(string text, TokenType tokenType)
    {
        var tokens = Lex(text);

        AssertNextToken(tokens, out var token);
        Assert.Equal(tokenType, token.Type);
        Assert.Equal(text, token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.EndOfInput, token.Type);
        Assert.Equal(string.Empty, token.Text);
        AssertNoTriviaOrDiagnostics(token);
    }

    [Theory]
    [InlineData("(", TokenType.ParenOpen)]
    [InlineData("[", TokenType.BracketOpen)]
    [InlineData("{", TokenType.CurlyOpen)]
    [InlineData(".", TokenType.Dot)]
    [InlineData(":", TokenType.Colon)]
    [InlineData(";", TokenType.Semicolon)]
    [Trait("Feature", "Punctuations")]
    public void TestPunctuation(string text, TokenType tokenType)
    {
        var tokens = Lex(text);

        AssertNextToken(tokens, out var token);
        Assert.Equal(tokenType, token.Type);
        Assert.Equal(text, token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.EndOfInput, token.Type);
        Assert.Equal(string.Empty, token.Text);
        AssertNoTriviaOrDiagnostics(token);
    }

    [Theory]
    [InlineData("+", TokenType.Plus)]
    [InlineData("-", TokenType.Minus)]
    [InlineData("*", TokenType.Star)]
    [InlineData("+=", TokenType.PlusAssign)]
    [InlineData("!=", TokenType.NotEqual)]
    [InlineData("=", TokenType.Assign)]
    [InlineData("==", TokenType.Equal)]
    [InlineData("mod", TokenType.KeywordMod)]
    [InlineData("rem", TokenType.KeywordRem)]
    [InlineData("and", TokenType.KeywordAnd)]
    [InlineData("not", TokenType.KeywordNot)]
    [Trait("Feature", "Operators")]
    public void TestOperator(string text, TokenType tokenType)
    {
        var tokens = Lex(text);

        AssertNextToken(tokens, out var token);
        Assert.Equal(tokenType, token.Type);
        Assert.Equal(text, token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.EndOfInput, token.Type);
        Assert.Equal(string.Empty, token.Text);
        AssertNoTriviaOrDiagnostics(token);
    }

    [Theory]
    [InlineData("0", TokenType.LiteralInteger)]
    [InlineData("123", TokenType.LiteralInteger)]
    [InlineData("12.3", TokenType.LiteralFloat)]
    [InlineData("true", TokenType.KeywordTrue)]
    [Trait("Feature", "Literals")]
    public void TestLiteral(string text, TokenType tokenType)
    {
        var tokens = Lex(text);

        AssertNextToken(tokens, out var token);
        Assert.Equal(tokenType, token.Type);
        Assert.Equal(text, token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.EndOfInput, token.Type);
        Assert.Equal(string.Empty, token.Text);
        AssertNoTriviaOrDiagnostics(token);
    }

    [Fact]
    [Trait("Feature", "Literals")]
    public void TestIntLiteralWithMethodCall()
    {
        string text = "56.MyFunction()";
        var tokens = Lex(text);

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.LiteralInteger, token.Type);
        Assert.Equal("56", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.Dot, token.Type);
        Assert.Equal(".", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.Identifier, token.Type);
        Assert.Equal("MyFunction", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.ParenOpen, token.Type);
        Assert.Equal("(", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.ParenClose, token.Type);
        Assert.Equal(")", token.Text);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.EndOfInput, token.Type);
        Assert.Equal(string.Empty, token.Text);
        AssertNoTriviaOrDiagnostics(token);
    }

    [Theory]
    [InlineData("'a'", "a")]
    [InlineData(@"'\\'", "\\")]
    [InlineData(@"'\''", "'")]
    [InlineData(@"'\""'", "\"")]
    [InlineData(@"'\n'", "\n")]
    [InlineData(@"'\u{3F}'", "?")]
    [InlineData(@"'\u{3f}'", "?")]
    [Trait("Feature", "Literals")]
    public void TestCharLiteral(string text, string charValue)
    {
        var tokens = Lex(text);

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.LiteralCharacter, token.Type);
        Assert.Equal(text, token.Text);
        Assert.Equal(charValue, token.ValueText);
        AssertNoTriviaOrDiagnostics(token);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.EndOfInput, token.Type);
        Assert.Equal(string.Empty, token.Text);
        AssertNoTriviaOrDiagnostics(token);
    }

    [Fact]
    public void TestUnclosedCharLiteral()
    {
        string text = "'a";
        var tokens = Lex(text);

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.LiteralCharacter, token.Type);
        Assert.Equal(text, token.Text);
        Assert.Equal("a", token.ValueText);
        AssertNoTrivia(token);
        Assert.Single(token.Diagnostics);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.EndOfInput, token.Type);
        Assert.Equal(string.Empty, token.Text);
        AssertNoTriviaOrDiagnostics(token);
    }

    [Fact]
    public void TestHelloWorld()
    {
        var text = """
            from System.Console import { WriteLine };

            func main() = WriteLine("Hello, World!");
            """;
        var tokens = Lex(text);

        AssertNextToken(tokens, out var token);
        Assert.Equal(TokenType.KeywordFrom, token.Type);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.Identifier, token.Type);
        Assert.Equal("System", token.Text);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.Dot, token.Type);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.Identifier, token.Type);
        Assert.Equal("Console", token.Text);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.KeywordImport, token.Type);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.CurlyOpen, token.Type);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.Identifier, token.Type);
        Assert.Equal("WriteLine", token.Text);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.CurlyClose, token.Type);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.Semicolon, token.Type);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.KeywordFunc, token.Type);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.Identifier, token.Type);
        Assert.Equal("main", token.Text);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.ParenOpen, token.Type);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.ParenClose, token.Type);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.Assign, token.Type);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.Identifier, token.Type);
        Assert.Equal("WriteLine", token.Text);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.ParenOpen, token.Type);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.LineStringStart, token.Type);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.StringContent, token.Type);
        Assert.Equal("Hello, World!", token.Text);
        Assert.Equal("Hello, World!", token.ValueText);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.LineStringEnd, token.Type);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.ParenClose, token.Type);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.Semicolon, token.Type);

        AssertNextToken(tokens, out token);
        Assert.Equal(TokenType.EndOfInput, token.Type);
    }
}
