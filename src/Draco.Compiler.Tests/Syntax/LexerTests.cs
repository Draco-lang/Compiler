using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Syntax;
using SyntaxToken = Draco.Compiler.Internal.Syntax.SyntaxToken;

namespace Draco.Compiler.Tests.Syntax;

public sealed class LexerTests
{
    private SyntaxToken Current => this.tokenEnumerator.Current;

    private IEnumerator<SyntaxToken> tokenEnumerator = Enumerable.Empty<SyntaxToken>().GetEnumerator();
    private readonly SyntaxDiagnosticTable diagnostics = new();

    private void Lex(string text)
    {
        var source = SourceReader.From(text);
        var lexer = new Lexer(source, this.diagnostics);
        this.tokenEnumerator = LexImpl(lexer).GetEnumerator();
    }

    private static IEnumerable<SyntaxToken> LexImpl(Lexer lexer)
    {
        while (true)
        {
            var token = lexer.Lex();
            yield return token;
            if (token.Kind == TokenKind.EndOfInput) break;
        }
    }

    private static string NormalizeNewliens(string text) => text
        .Replace("\r\n", "\n")
        .Replace("\r", "\n");

    private void AssertNextToken() => Assert.True(this.tokenEnumerator.MoveNext());

    private void AssertNextToken(TokenKind type, string text = "", object? value = null)
    {
        this.AssertNextToken();
        this.AssertType(type);
        this.AssertText(text);
        this.AssertValue(value);
    }

    private void AssertNoTrivia()
    {
        Assert.Empty(this.Current.LeadingTrivia);
        Assert.Empty(this.Current.TrailingTrivia);
    }

    private void AssertNoTriviaOrDiagnostics()
    {
        this.AssertNoTrivia();
        this.AssertDiagnostics();
    }

    private void AssertType(TokenKind type) => Assert.Equal(type, this.Current.Kind);
    private void AssertText(string text) => Assert.Equal(text, this.Current.Text);
    private void AssertValue(object? value)
    {
        if (value is double d)
        {
            Assert.NotNull(this.Current.Value);
            Assert.Equal(d, (double)this.Current.Value!, 5);
        }
        else
        {
            Assert.Equal(value, this.Current.Value);
        }
    }
    private void AssertValueText(string? text) => Assert.Equal(text, this.Current.ValueText);

    private void AssertLeadingTrivia(params (TriviaKind Type, string Text)[] trivia)
    {
        Assert.Equal(trivia.Length, this.Current.LeadingTrivia.Count);
        Assert.True(this.Current.LeadingTrivia.Select(t => (t.Kind, t.Text)).SequenceEqual(trivia));
    }

    private void AssertTrailingTrivia(params (TriviaKind Type, string Text)[] trivia)
    {
        Assert.Equal(trivia.Length, this.Current.TrailingTrivia.Count);
        Assert.True(this.Current.TrailingTrivia.Select(t => (t.Kind, t.Text)).SequenceEqual(trivia));
    }

    private void AssertDiagnostics(params DiagnosticTemplate[] diags)
    {
        var gotDiags = this.diagnostics.Get(this.Current);
        Assert.Equal(diags.Length, gotDiags!.Count);
        Assert.True(diags.SequenceEqual(gotDiags.Select(d => d.Info.Template)));
    }

    [Fact]
    [Trait("Feature", "Comments")]
    public void TestLineComment()
    {
        var text = "// Hello, comments";
        this.Lex(text);

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertLeadingTrivia((TriviaKind.LineComment, "// Hello, comments"));
        this.AssertTrailingTrivia();
        this.AssertDiagnostics();
    }

    [Fact]
    [Trait("Feature", "Comments")]
    public void TestMultilineDocumentationComment()
    {
        var text = """
        /// Hello,
        /// multiline doc comments
        """;
        this.Lex(NormalizeNewliens(text));

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertDiagnostics();
        this.AssertLeadingTrivia(
            (TriviaKind.DocumentationComment, "/// Hello,"),
            (TriviaKind.Newline, "\n"),
            (TriviaKind.DocumentationComment, "/// multiline doc comments"));
        this.AssertTrailingTrivia();
    }

    [Fact]
    [Trait("Feature", "Comments")]
    public void TestSinglelineDocumentationComment()
    {
        var text = "/// Hello, doc comments";
        this.Lex(text);

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertLeadingTrivia((TriviaKind.DocumentationComment, "/// Hello, doc comments"));
        this.AssertTrailingTrivia();
        this.AssertDiagnostics();
    }

    [Fact]
    [Trait("Feature", "Strings")]
    public void TestLineString()
    {
        var text = """
            "Hello, line strings!"
            """;
        this.Lex(text);

        this.AssertNextToken(TokenKind.LineStringStart, "\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.StringContent, "Hello, line strings!", "Hello, line strings!");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.LineStringEnd, "\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
    }

    [Fact]
    [Trait("Feature", "Strings")]
    public void TestUnclosedLineString()
    {
        var text = """
            "Hello, line strings!
            """;
        this.Lex(text);

        this.AssertNextToken(TokenKind.LineStringStart, "\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.StringContent, "Hello, line strings!", "Hello, line strings!");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
    }

    [Fact]
    [Trait("Feature", "Strings")]
    public void TestUnclosedLineStringWithNewlineAfter()
    {
        var text = """
        "Hello, line strings!

        """;
        this.Lex(NormalizeNewliens(text));

        this.AssertNextToken(TokenKind.LineStringStart, "\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.StringContent, "Hello, line strings!", "Hello, line strings!");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertLeadingTrivia((TriviaKind.Newline, "\n"));
        this.AssertTrailingTrivia();
        this.AssertDiagnostics();
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
        this.Lex(text);

        this.AssertNextToken(TokenKind.LineStringStart, $"{ext}\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(
            TokenKind.StringContent,
            @$"\{ext}""\{ext}\\{ext}n\{ext}'\{ext}u{{1F47D}}\{ext}0",
            "\"\\\n'👽\0");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.LineStringEnd, $"\"{ext}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
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
        this.Lex(text);

        this.AssertNextToken();
        Assert.Equal(TokenKind.LineStringStart, this.Current.Kind);
        Assert.Equal($"{ext}\"", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.StringContent, @$"\{ext}u{{}}", string.Empty);
        this.AssertNoTrivia();
        this.AssertDiagnostics(SyntaxErrors.ZeroLengthUnicodeCodepoint);

        this.AssertNextToken(TokenKind.LineStringEnd, $"\"{ext}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
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
        this.Lex(text);

        this.AssertNextToken(TokenKind.LineStringStart, $"{ext}\"");
        this.AssertNoTriviaOrDiagnostics();

        //TODO: change this when we get better errors out of invalid unicode codepoints
        this.AssertNextToken(TokenKind.StringContent, @$"\{ext}u{{3S}}", "S}");
        this.AssertNoTrivia();
        this.AssertDiagnostics(SyntaxErrors.UnclosedUnicodeCodepoint);

        this.AssertNextToken(TokenKind.LineStringEnd, $"\"{ext}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
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
        this.Lex(text);

        this.AssertNextToken(TokenKind.LineStringStart, $"{ext}\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.StringContent, @$"\{ext}u{{", string.Empty);
        this.AssertNoTrivia();
        this.AssertDiagnostics(SyntaxErrors.UnclosedUnicodeCodepoint);

        this.AssertNextToken(TokenKind.LineStringEnd, $"\"{ext}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
    }

    [Fact]
    [Trait("Feature", "Strings")]
    public void TestLineStringMixedEscapes()
    {
        var text = $$"""
            ##"\a\#n\#u{123}\##t"##
            """;
        this.Lex(text);

        this.AssertNextToken(TokenKind.LineStringStart, "##\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(
            TokenKind.StringContent,
            @"\a\#n\#u{123}\##t",
            "\\a\\#n\\#u{123}\t");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.LineStringEnd, $"\"##");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
    }

    [Fact]
    [Trait("Feature", "Strings")]
    public void TestLineStringWithSpacesAround()
    {
        var text = """
            "  hello   "
            """;
        this.Lex(text);

        this.AssertNextToken(TokenKind.LineStringStart, "\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(
            TokenKind.StringContent,
            "  hello   ",
            "  hello   ");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.LineStringEnd, "\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
    }

    [Fact]
    [Trait("Feature", "Strings")]
    public void TestMultilineStringWithSpacesAround()
    {
        const string spaceAfter = "   ";
        var text = $""""
            """
                  hello{spaceAfter}
                """
            """";
        this.Lex(NormalizeNewliens(text));

        this.AssertNextToken(TokenKind.MultiLineStringStart, "\"\"\"");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia(
            (TriviaKind.Newline, "\n"));
        this.AssertDiagnostics();

        this.AssertNextToken(
            TokenKind.StringContent,
            $"      hello{spaceAfter}",
            $"      hello{spaceAfter}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.MultiLineStringEnd, "\"\"\"");
        this.AssertLeadingTrivia(
            (TriviaKind.Newline, "\n"),
            (TriviaKind.Whitespace, "    "));
        this.AssertTrailingTrivia();
        this.AssertDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
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
        this.Lex(text);

        this.AssertNextToken(TokenKind.LineStringStart, $"{ext}\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.StringContent, @$"\{ext}y", "y");
        this.AssertNoTrivia();
        this.AssertDiagnostics(SyntaxErrors.IllegalEscapeCharacter);

        this.AssertNextToken(TokenKind.LineStringEnd, $"\"{ext}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
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
        this.Lex(NormalizeNewliens(text));

        this.AssertNextToken(TokenKind.MultiLineStringStart, $"{ext}{quotes}");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaKind.Newline, "\n"));
        this.AssertDiagnostics();

        this.AssertNextToken(
            TokenKind.StringContent,
            "    Hello!",
            "    Hello!");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.StringNewline, "\n", "\n");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(
            TokenKind.StringContent,
            "    Bye!",
            "    Bye!");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.MultiLineStringEnd, $"{quotes}{ext}");
        this.AssertLeadingTrivia(
            (TriviaKind.Newline, "\n"),
            (TriviaKind.Whitespace, "    "));
        this.AssertTrailingTrivia();
        this.AssertDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
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
        this.Lex(NormalizeNewliens(text));

        this.AssertNextToken(TokenKind.MultiLineStringStart, $"{ext}{quotes}");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaKind.Newline, "\n"));
        this.AssertDiagnostics();

        this.AssertNextToken(TokenKind.MultiLineStringEnd, $"{quotes}{ext}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
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
        this.Lex(NormalizeNewliens(text));

        this.AssertNextToken(TokenKind.MultiLineStringStart, $"{ext}{quotes}");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaKind.Newline, "\n"));
        this.AssertDiagnostics();

        this.AssertNextToken(TokenKind.MultiLineStringEnd, $"{quotes}{ext}");
        this.AssertLeadingTrivia((TriviaKind.Whitespace, "    "));
        this.AssertTrailingTrivia();
        this.AssertDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
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
        this.Lex(NormalizeNewliens(text));

        this.AssertNextToken(TokenKind.MultiLineStringStart, $"{ext}{quotes}");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaKind.Whitespace, "    "));
        this.AssertDiagnostics();

        this.AssertNextToken(TokenKind.MultiLineStringEnd, $"{quotes}{ext}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
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
        this.Lex(NormalizeNewliens(text));

        this.AssertNextToken(TokenKind.MultiLineStringStart, $"{ext}{quotes}");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaKind.Whitespace, " "));
        this.AssertDiagnostics();

        this.AssertNextToken(TokenKind.StringContent, "hello", "hello");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.MultiLineStringEnd, $"{quotes}{ext}");
        this.AssertLeadingTrivia((TriviaKind.Whitespace, " "));
        this.AssertTrailingTrivia();
        this.AssertDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
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
        this.Lex(NormalizeNewliens(text));

        this.AssertNextToken(TokenKind.MultiLineStringStart, $"{ext}{quotes}");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaKind.Newline, "\n"));
        this.AssertDiagnostics();

        this.AssertNextToken(
            TokenKind.StringContent,
            "    Hello!",
            "    Hello!");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.StringNewline, "\n", "\n");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(
            TokenKind.StringContent,
            "    Bye!",
            "    Bye!");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.MultiLineStringEnd, $"{quotes}{ext}");
        this.AssertLeadingTrivia((TriviaKind.Newline, "\n"));
        this.AssertTrailingTrivia();
        this.AssertDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
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
        this.Lex(NormalizeNewliens(text));

        this.AssertNextToken(TokenKind.MultiLineStringStart, $"{ext}{quotes}");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaKind.Newline, "\n"));
        this.AssertDiagnostics();

        this.AssertNextToken(
            TokenKind.StringContent,
            "    Hello!",
            "    Hello!");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.StringNewline, $"\\{ext}\n", string.Empty);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(
            TokenKind.StringContent,
            "    Bye!",
            "    Bye!");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.MultiLineStringEnd, $"{quotes}{ext}");
        this.AssertLeadingTrivia(
            (TriviaKind.Newline, "\n"),
            (TriviaKind.Whitespace, "    "));
        this.AssertTrailingTrivia();
        this.AssertDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
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
        this.Lex(NormalizeNewliens(text));

        this.AssertNextToken(TokenKind.MultiLineStringStart, $"{ext}{quotes}");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaKind.Newline, "\n"));
        this.AssertDiagnostics();

        this.AssertNextToken(
            TokenKind.StringContent,
            "    Hello!",
            "    Hello!");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.StringNewline, $"\\{ext}{trailingSpace}\n", string.Empty);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(
            TokenKind.StringContent,
            "    Bye!",
            "    Bye!");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.MultiLineStringEnd, $"{quotes}{ext}");
        this.AssertLeadingTrivia(
            (TriviaKind.Newline, "\n"),
            (TriviaKind.Whitespace, "    "));
        this.AssertTrailingTrivia();
        this.AssertDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
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
        this.Lex(text);

        this.AssertNextToken(TokenKind.LineStringStart, $"{ext}\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.StringContent, "x = ", "x = ");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.InterpolationStart, $@"\{ext}{{");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.Identifier, "x", "x");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.InterpolationEnd, "}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.StringContent, ", x + y = ", ", x + y = ");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.InterpolationStart, $@"\{ext}{{");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaKind.Whitespace, " "));
        this.AssertDiagnostics();

        this.AssertNextToken(TokenKind.Identifier, "x", "x");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaKind.Whitespace, " "));
        this.AssertDiagnostics();

        this.AssertNextToken(TokenKind.Plus, "+");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaKind.Whitespace, " "));
        this.AssertDiagnostics();

        this.AssertNextToken(TokenKind.Identifier, "y", "y");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaKind.Whitespace, " "));
        this.AssertDiagnostics();

        this.AssertNextToken(TokenKind.InterpolationEnd, "}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.StringContent, ", y = ", ", y = ");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.InterpolationStart, $@"\{ext}{{");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaKind.Whitespace, " "));
        this.AssertDiagnostics();

        this.AssertNextToken(TokenKind.CurlyOpen, "{");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.Identifier, "y", "y");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.CurlyClose, "}");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaKind.Whitespace, " "));
        this.AssertDiagnostics();

        this.AssertNextToken(TokenKind.InterpolationEnd, "}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.LineStringEnd, $"\"{ext}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
    }

    [Fact]
    [Trait("Feature", "Strings")]
    public void TestNewlineInLineStringInterpolation()
    {
        // "hello\{
        // var\}bye"
        var text = "\"hello\\{\nvar}bye\"";
        this.Lex(text);

        this.AssertNextToken(TokenKind.LineStringStart, "\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.StringContent, "hello", "hello");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.InterpolationStart, @"\{");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaKind.Newline, "\n"));
        this.AssertDiagnostics();

        this.AssertNextToken(TokenKind.KeywordVar, "var");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.CurlyClose, "}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.Identifier, "bye", "bye");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.LineStringStart, "\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
    }

    [Fact]
    [Trait("Feature", "Strings")]
    public void TestNewlineInLineStringInterpolationNestedString()
    {
        // "hello\{"bye
        // var\}baz"
        var text = "\"hello\\{\"bye\nvar}baz\"";
        this.Lex(text);

        this.AssertNextToken(TokenKind.LineStringStart, "\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.StringContent, "hello", "hello");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.InterpolationStart, @"\{");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.LineStringStart, "\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.StringContent, "bye", "bye");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.KeywordVar, "var");
        this.AssertLeadingTrivia((TriviaKind.Newline, "\n"));
        this.AssertTrailingTrivia();
        this.AssertDiagnostics();

        this.AssertNextToken(TokenKind.CurlyClose, "}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.Identifier, "baz", "baz");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.LineStringStart, "\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
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
        this.Lex(text);

        this.AssertNextToken(TokenKind.MultiLineStringStart, quotes);
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaKind.Newline, "\n"));
        this.AssertDiagnostics();

        this.AssertNextToken(TokenKind.StringContent, "foo", "foo");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.InterpolationStart, @"\{");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaKind.Newline, "\n"));
        this.AssertDiagnostics();

        this.AssertNextToken(TokenKind.Identifier, "x", "x");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.InterpolationEnd, "}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.StringContent, "bar", "bar");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.MultiLineStringEnd, quotes);
        this.AssertLeadingTrivia((TriviaKind.Newline, "\n"));
        this.AssertTrailingTrivia();
        this.AssertDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
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
        this.Lex(text);

        this.AssertNextToken(TokenKind.MultiLineStringStart, quotes);
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaKind.Newline, "\n"));
        this.AssertDiagnostics();

        this.AssertNextToken(TokenKind.StringContent, "foo", "foo");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.InterpolationStart, @"\{");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.LineStringStart, "\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.StringContent, "bar", "bar");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.Identifier, "x", "x");
        this.AssertLeadingTrivia((TriviaKind.Newline, "\n"));
        this.AssertTrailingTrivia();
        this.AssertDiagnostics();

        this.AssertNextToken(TokenKind.InterpolationEnd, "}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.StringContent, "baz", "baz");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.MultiLineStringEnd, quotes);
        this.AssertLeadingTrivia((TriviaKind.Newline, "\n"));
        this.AssertTrailingTrivia();
        this.AssertDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
    }

    [Theory]
    [InlineData("")]
    [InlineData("#")]
    [InlineData("##")]
    [InlineData("###")]
    [Trait("Feature", "Strings")]
    public void TestEndOfInputAfterEscapeSequenceStart(string ext)
    {
        var text = $"""
            {ext}"\{ext}
            """;
        this.Lex(text);

        this.AssertNextToken(TokenKind.LineStringStart, $"{ext}\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.StringContent, $"\\{ext}", $"\\{ext}");
        this.AssertNoTrivia();
        this.AssertDiagnostics(SyntaxErrors.UnexpectedEscapeSequenceEnd);

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
    }

    [Theory]
    [InlineData("")]
    [InlineData("#")]
    [InlineData("##")]
    [InlineData("###")]
    [Trait("Feature", "Strings")]
    public void TestEndOfInputAfterEscapeSequenceStartAndWhitespace(string ext)
    {
        var space = " ";
        var text = $"""
            {ext}"\{ext}{space}
            """;
        this.Lex(text);

        this.AssertNextToken(TokenKind.LineStringStart, $"{ext}\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.StringContent, $"\\{ext}{space}", $"{space}");
        this.AssertNoTrivia();
        this.AssertDiagnostics(SyntaxErrors.IllegalEscapeCharacter);

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
    }

    [Theory]
    [InlineData("#")]
    [InlineData("##")]
    [InlineData("###")]
    [Trait("Feature", "Strings")]
    public void TestEndOfInputAfterEscapeSequenceStartLessDelimitersThanOnStringStart(string ext)
    {
        var text = $"""
            {ext}#"\{ext}
            """;
        this.Lex(text);

        this.AssertNextToken(TokenKind.LineStringStart, $"{ext}#\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.StringContent, $"\\{ext}", $"\\{ext}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
    }

    [Theory]
    [InlineData("if", TokenKind.KeywordIf)]
    [InlineData("else", TokenKind.KeywordElse)]
    [InlineData("while", TokenKind.KeywordWhile)]
    [InlineData("if_", TokenKind.Identifier)]
    [InlineData("ifa", TokenKind.Identifier)]
    [InlineData("if0", TokenKind.Identifier)]
    [InlineData("_if", TokenKind.Identifier)]
    [InlineData("hello", TokenKind.Identifier)]
    [InlineData("hello123", TokenKind.Identifier)]
    [InlineData("hello_123", TokenKind.Identifier)]
    [InlineData("_hello_123", TokenKind.Identifier)]
    [Trait("Feature", "Words")]
    public void TestKeyword(string text, TokenKind tokenKind)
    {
        this.Lex(text);

        this.AssertNextToken(tokenKind, text, tokenKind == TokenKind.Identifier ? text : null);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
    }

    [Theory]
    [InlineData("(", TokenKind.ParenOpen)]
    [InlineData("[", TokenKind.BracketOpen)]
    [InlineData("{", TokenKind.CurlyOpen)]
    [InlineData(".", TokenKind.Dot)]
    [InlineData(":", TokenKind.Colon)]
    [InlineData(";", TokenKind.Semicolon)]
    [Trait("Feature", "Punctuations")]
    public void TestPunctuation(string text, TokenKind tokenKind)
    {
        this.Lex(text);

        this.AssertNextToken(tokenKind, text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
    }

    [Theory]
    [InlineData("+", TokenKind.Plus)]
    [InlineData("-", TokenKind.Minus)]
    [InlineData("*", TokenKind.Star)]
    [InlineData("+=", TokenKind.PlusAssign)]
    [InlineData("!=", TokenKind.NotEqual)]
    [InlineData("=", TokenKind.Assign)]
    [InlineData("==", TokenKind.Equal)]
    [InlineData("mod", TokenKind.KeywordMod)]
    [InlineData("rem", TokenKind.KeywordRem)]
    [InlineData("and", TokenKind.KeywordAnd)]
    [InlineData("not", TokenKind.KeywordNot)]
    [Trait("Feature", "Operators")]
    public void TestOperator(string text, TokenKind tokenKind)
    {
        this.Lex(text);

        this.AssertNextToken(tokenKind, text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
    }

    [Theory]
    [InlineData("0", 0, TokenKind.LiteralInteger)]
    [InlineData("123", 123, TokenKind.LiteralInteger)]
    [InlineData("12.3", 12.3, TokenKind.LiteralFloat)]
    [InlineData("0x4c6", 1222, TokenKind.LiteralInteger)]
    [InlineData("0b110101", 53, TokenKind.LiteralInteger)]
    [InlineData("10E3", 10000d, TokenKind.LiteralFloat)]
    [InlineData("10E-3", 0.01, TokenKind.LiteralFloat)]
    [InlineData("0.1e+4", 1000d, TokenKind.LiteralFloat)]
    [InlineData("123.345E-12", 1.23345E-10, TokenKind.LiteralFloat)]
    [Trait("Feature", "Literals")]
    public void TestNumericLiterals(string text, object value, TokenKind tokenKind)
    {
        this.Lex(text);

        this.AssertNextToken(tokenKind, text, value);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
    }

    [Theory]
    [InlineData("true", true, TokenKind.KeywordTrue)]
    [InlineData("false", false, TokenKind.KeywordFalse)]
    [Trait("Feature", "Literals")]
    public void TestBoolLiterals(string text, bool value, TokenKind tokenKind)
    {
        this.Lex(text);

        this.AssertNextToken(tokenKind, text, value);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
    }

    [Theory]
    [InlineData("0.1e+", TokenKind.LiteralFloat)]
    [InlineData("345E-", TokenKind.LiteralFloat)]
    [Trait("Feature", "Literals")]
    public void TestNumericLiteralInvalidNonDecimalFormats(string text, TokenKind tokenKind)
    {
        this.Lex(text);

        this.AssertNextToken(tokenKind, text);
        this.AssertDiagnostics(SyntaxErrors.UnexpectedFloatingPointLiteralEnd);

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
    }

    [Fact]
    [Trait("Feature", "Literals")]
    public void TestIntLiteralWithMethodCall()
    {
        var text = "56.MyFunction()";
        this.Lex(text);

        this.AssertNextToken(TokenKind.LiteralInteger, "56", 56);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.Dot, ".");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.Identifier, "MyFunction", "MyFunction");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.ParenOpen, "(");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.ParenClose, ")");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
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
        this.Lex(text);

        this.AssertNextToken(TokenKind.LiteralCharacter, text, charValue);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
    }

    [Fact]
    [Trait("Feature", "Literals")]
    public void TestUnclosedCharLiteral()
    {
        var text = "'a";
        this.Lex(text);

        this.AssertNextToken(TokenKind.LiteralCharacter, text, "a");
        this.AssertNoTrivia();
        this.AssertDiagnostics(SyntaxErrors.UnclosedCharacterLiteral);

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
    }

    [Fact]
    [Trait("Feature", "Literals")]
    public void TestEndOfInputAfterSingleQuote()
    {
        var text = "'";
        this.Lex(text);

        this.AssertNextToken(TokenKind.LiteralCharacter, text, ' ');
        this.AssertNoTrivia();
        this.AssertDiagnostics(SyntaxErrors.UnexpectedCharacterLiteralEnd);

        this.AssertNextToken(TokenKind.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
    }

    [Fact]
    public void TestHelloWorld()
    {
        var text = """
            import System.Console;

            func main() = WriteLine("Hello, World!");
            """;
        this.Lex(text);

        this.AssertNextToken(TokenKind.KeywordImport, "import");
        this.AssertNextToken(TokenKind.Identifier, "System", "System");
        this.AssertNextToken(TokenKind.Dot, ".");
        this.AssertNextToken(TokenKind.Identifier, "Console", "Console");
        this.AssertNextToken(TokenKind.Semicolon, ";");
        this.AssertNextToken(TokenKind.KeywordFunc, "func");
        this.AssertNextToken(TokenKind.Identifier, "main", "main");
        this.AssertNextToken(TokenKind.ParenOpen, "(");
        this.AssertNextToken(TokenKind.ParenClose, ")");
        this.AssertNextToken(TokenKind.Assign, "=");
        this.AssertNextToken(TokenKind.Identifier, "WriteLine", "WriteLine");
        this.AssertNextToken(TokenKind.ParenOpen, "(");
        this.AssertNextToken(TokenKind.LineStringStart, "\"");
        this.AssertNextToken(TokenKind.StringContent, "Hello, World!", "Hello, World!");
        this.AssertNextToken(TokenKind.LineStringEnd, "\"");
        this.AssertNextToken(TokenKind.ParenClose, ")");
        this.AssertNextToken(TokenKind.Semicolon, ";");
        this.AssertNextToken(TokenKind.EndOfInput);
    }
}
