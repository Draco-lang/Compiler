using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Syntax;
using Diagnostic = Draco.Compiler.Internal.Diagnostics.Diagnostic;
using SyntaxToken = Draco.Compiler.Internal.Syntax.SyntaxToken;

namespace Draco.Compiler.Tests.Syntax;

public sealed class LexerTests
{
    private SyntaxToken Current => this.tokenEnumerator.Current;

    private IEnumerator<SyntaxToken> tokenEnumerator = Enumerable.Empty<SyntaxToken>().GetEnumerator();
    private ConditionalWeakTable<SyntaxToken, IImmutableList<Diagnostic>> diagnostics = new();

    private void Lex(string text)
    {
        var source = SourceReader.From(text);
        var lexer = new Lexer(source);
        this.tokenEnumerator = LexImpl(lexer).GetEnumerator();
        this.diagnostics = lexer.Diagnostics;
    }

    private static IEnumerable<SyntaxToken> LexImpl(Lexer lexer)
    {
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

    private void AssertNextToken() => Assert.True(this.tokenEnumerator.MoveNext());

    private void AssertNextToken(TokenType type, string text = "", string? valueText = null)
    {
        this.AssertNextToken();
        this.AssertType(type);
        this.AssertText(text);
        this.AssertValueText(valueText);
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

    private void AssertType(TokenType type) => Assert.Equal(type, this.Current.Type);
    private void AssertText(string text) => Assert.Equal(text, this.Current.Text);
    private void AssertValueText(string? text) => Assert.Equal(text, this.Current.ValueText);

    private void AssertLeadingTrivia(params (TriviaType Type, string Text)[] trivia)
    {
        Assert.Equal(trivia.Length, this.Current.LeadingTrivia.Length);
        Assert.True(this.Current.LeadingTrivia.Select(t => (t.Type, t.Text)).SequenceEqual(trivia));
    }

    private void AssertTrailingTrivia(params (TriviaType Type, string Text)[] trivia)
    {
        Assert.Equal(trivia.Length, this.Current.TrailingTrivia.Length);
        Assert.True(this.Current.TrailingTrivia.Select(t => (t.Type, t.Text)).SequenceEqual(trivia));
    }

    private void AssertDiagnostics(params DiagnosticTemplate[] diags)
    {
        if (diags.Length == 0)
        {
            Assert.False(this.diagnostics.TryGetValue(this.Current, out _));
        }
        else
        {
            Assert.True(this.diagnostics.TryGetValue(this.Current, out var gotDiags));
            Assert.Equal(gotDiags!.Count, diags.Length);
            Assert.True(diags.SequenceEqual(gotDiags.Select(d => d.Template)));
        }
    }

    [Fact]
    [Trait("Feature", "Comments")]
    public void TestLineComment()
    {
        var text = "// Hello, comments";
        this.Lex(text);

        this.AssertNextToken(TokenType.EndOfInput);
        this.AssertLeadingTrivia((TriviaType.LineComment, "// Hello, comments"));
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

        this.AssertNextToken(TokenType.EndOfInput);
        this.AssertDiagnostics();
        this.AssertLeadingTrivia(
            (TriviaType.DocumentationComment, "/// Hello,"),
            (TriviaType.Newline, "\n"),
            (TriviaType.DocumentationComment, "/// multiline doc comments"));
        this.AssertTrailingTrivia();
    }

    [Fact]
    [Trait("Feature", "Comments")]
    public void TestSinglelineDocumentationComment()
    {
        var text = "/// Hello, doc comments";
        this.Lex(text);

        this.AssertNextToken(TokenType.EndOfInput);
        this.AssertLeadingTrivia((TriviaType.DocumentationComment, "/// Hello, doc comments"));
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

        this.AssertNextToken(TokenType.LineStringStart, "\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.StringContent, "Hello, line strings!", "Hello, line strings!");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.LineStringEnd, "\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.EndOfInput);
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

        this.AssertNextToken(TokenType.LineStringStart, "\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.StringContent, "Hello, line strings!", "Hello, line strings!");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.EndOfInput);
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

        this.AssertNextToken(TokenType.LineStringStart, "\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.StringContent, "Hello, line strings!", "Hello, line strings!");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.EndOfInput);
        this.AssertLeadingTrivia((TriviaType.Newline, "\n"));
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

        this.AssertNextToken(TokenType.LineStringStart, $"{ext}\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(
            TokenType.StringContent,
            @$"\{ext}""\{ext}\\{ext}n\{ext}'\{ext}u{{1F47D}}\{ext}0",
            "\"\\\n'👽\0");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.LineStringEnd, $"\"{ext}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.EndOfInput);
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
        Assert.Equal(TokenType.LineStringStart, this.Current.Type);
        Assert.Equal($"{ext}\"", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.StringContent, @$"\{ext}u{{}}", string.Empty);
        this.AssertNoTrivia();
        this.AssertDiagnostics(SyntaxErrors.ZeroLengthUnicodeCodepoint);

        this.AssertNextToken(TokenType.LineStringEnd, $"\"{ext}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.EndOfInput);
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

        this.AssertNextToken(TokenType.LineStringStart, $"{ext}\"");
        this.AssertNoTriviaOrDiagnostics();

        //TODO: change this when we get better errors out of invalid unicode codepoints
        this.AssertNextToken(TokenType.StringContent, @$"\{ext}u{{3S}}", "S}");
        this.AssertNoTrivia();
        this.AssertDiagnostics(SyntaxErrors.UnclosedUnicodeCodepoint);

        this.AssertNextToken(TokenType.LineStringEnd, $"\"{ext}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.EndOfInput);
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

        this.AssertNextToken(TokenType.LineStringStart, $"{ext}\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.StringContent, @$"\{ext}u{{", string.Empty);
        this.AssertNoTrivia();
        this.AssertDiagnostics(SyntaxErrors.UnclosedUnicodeCodepoint);

        this.AssertNextToken(TokenType.LineStringEnd, $"\"{ext}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.EndOfInput);
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

        this.AssertNextToken(TokenType.LineStringStart, "##\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(
            TokenType.StringContent,
            @"\a\#n\#u{123}\##t",
            "\\a\\#n\\#u{123}\t");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.LineStringEnd, $"\"##");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.EndOfInput);
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

        this.AssertNextToken(TokenType.LineStringStart, $"{ext}\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.StringContent, @$"\{ext}y", "y");
        this.AssertNoTrivia();
        this.AssertDiagnostics(SyntaxErrors.IllegalEscapeCharacter);

        this.AssertNextToken(TokenType.LineStringEnd, $"\"{ext}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.EndOfInput);
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

        this.AssertNextToken(TokenType.MultiLineStringStart, $"{ext}{quotes}");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaType.Newline, "\n"));
        this.AssertDiagnostics();

        this.AssertNextToken(
            TokenType.StringContent,
            "    Hello!",
            "    Hello!");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.StringNewline, "\n", "\n");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(
            TokenType.StringContent,
            "    Bye!",
            "    Bye!");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.MultiLineStringEnd, $"{quotes}{ext}");
        this.AssertLeadingTrivia(
            (TriviaType.Newline, "\n"),
            (TriviaType.Whitespace, "    "));
        this.AssertTrailingTrivia();
        this.AssertDiagnostics();

        this.AssertNextToken(TokenType.EndOfInput);
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

        this.AssertNextToken(TokenType.MultiLineStringStart, $"{ext}{quotes}");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaType.Newline, "\n"));
        this.AssertDiagnostics();

        this.AssertNextToken(TokenType.MultiLineStringEnd, $"{quotes}{ext}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.EndOfInput);
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

        this.AssertNextToken(TokenType.MultiLineStringStart, $"{ext}{quotes}");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaType.Newline, "\n"));
        this.AssertDiagnostics();

        this.AssertNextToken(TokenType.MultiLineStringEnd, $"{quotes}{ext}");
        this.AssertLeadingTrivia((TriviaType.Whitespace, "    "));
        this.AssertTrailingTrivia();
        this.AssertDiagnostics();

        this.AssertNextToken(TokenType.EndOfInput);
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

        this.AssertNextToken(TokenType.MultiLineStringStart, $"{ext}{quotes}");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaType.Whitespace, "    "));
        this.AssertDiagnostics();

        this.AssertNextToken(TokenType.MultiLineStringEnd, $"{quotes}{ext}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.EndOfInput);
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

        this.AssertNextToken(TokenType.MultiLineStringStart, $"{ext}{quotes}");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaType.Whitespace, " "));
        this.AssertDiagnostics();

        this.AssertNextToken(TokenType.StringContent, "hello", "hello");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.MultiLineStringEnd, $"{quotes}{ext}");
        this.AssertLeadingTrivia((TriviaType.Whitespace, " "));
        this.AssertTrailingTrivia();
        this.AssertDiagnostics();

        this.AssertNextToken(TokenType.EndOfInput);
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

        this.AssertNextToken(TokenType.MultiLineStringStart, $"{ext}{quotes}");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaType.Newline, "\n"));
        this.AssertDiagnostics();

        this.AssertNextToken(
            TokenType.StringContent,
            "    Hello!",
            "    Hello!");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.StringNewline, "\n", "\n");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(
            TokenType.StringContent,
            "    Bye!",
            "    Bye!");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.MultiLineStringEnd, $"{quotes}{ext}");
        this.AssertLeadingTrivia((TriviaType.Newline, "\n"));
        this.AssertTrailingTrivia();
        this.AssertDiagnostics();

        this.AssertNextToken(TokenType.EndOfInput);
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

        this.AssertNextToken(TokenType.MultiLineStringStart, $"{ext}{quotes}");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaType.Newline, "\n"));
        this.AssertDiagnostics();

        this.AssertNextToken(
            TokenType.StringContent,
            "    Hello!",
            "    Hello!");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.StringNewline, $"\\{ext}\n", string.Empty);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(
            TokenType.StringContent,
            "    Bye!",
            "    Bye!");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.MultiLineStringEnd, $"{quotes}{ext}");
        this.AssertLeadingTrivia(
            (TriviaType.Newline, "\n"),
            (TriviaType.Whitespace, "    "));
        this.AssertTrailingTrivia();
        this.AssertDiagnostics();

        this.AssertNextToken(TokenType.EndOfInput);
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

        this.AssertNextToken(TokenType.MultiLineStringStart, $"{ext}{quotes}");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaType.Newline, "\n"));
        this.AssertDiagnostics();

        this.AssertNextToken(
            TokenType.StringContent,
            "    Hello!",
            "    Hello!");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.StringNewline, $"\\{ext}{trailingSpace}\n", string.Empty);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(
            TokenType.StringContent,
            "    Bye!",
            "    Bye!");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.MultiLineStringEnd, $"{quotes}{ext}");
        this.AssertLeadingTrivia(
            (TriviaType.Newline, "\n"),
            (TriviaType.Whitespace, "    "));
        this.AssertTrailingTrivia();
        this.AssertDiagnostics();

        this.AssertNextToken(TokenType.EndOfInput);
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

        this.AssertNextToken(TokenType.LineStringStart, $"{ext}\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.StringContent, "x = ", "x = ");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.InterpolationStart, $@"\{ext}{{");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.Identifier, "x", "x");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.InterpolationEnd, "}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.StringContent, ", x + y = ", ", x + y = ");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.InterpolationStart, $@"\{ext}{{");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaType.Whitespace, " "));
        this.AssertDiagnostics();

        this.AssertNextToken(TokenType.Identifier, "x", "x");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaType.Whitespace, " "));
        this.AssertDiagnostics();

        this.AssertNextToken(TokenType.Plus, "+");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaType.Whitespace, " "));
        this.AssertDiagnostics();

        this.AssertNextToken(TokenType.Identifier, "y", "y");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaType.Whitespace, " "));
        this.AssertDiagnostics();

        this.AssertNextToken(TokenType.InterpolationEnd, "}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.StringContent, ", y = ", ", y = ");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.InterpolationStart, $@"\{ext}{{");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaType.Whitespace, " "));
        this.AssertDiagnostics();

        this.AssertNextToken(TokenType.CurlyOpen, "{");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.Identifier, "y", "y");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.CurlyClose, "}");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaType.Whitespace, " "));
        this.AssertDiagnostics();

        this.AssertNextToken(TokenType.InterpolationEnd, "}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.LineStringEnd, $"\"{ext}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.EndOfInput);
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

        this.AssertNextToken(TokenType.LineStringStart, "\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.StringContent, "hello", "hello");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.InterpolationStart, @"\{");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaType.Newline, "\n"));
        this.AssertDiagnostics();

        this.AssertNextToken(TokenType.KeywordVar, "var");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.CurlyClose, "}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.Identifier, "bye", "bye");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.LineStringStart, "\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.EndOfInput);
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

        this.AssertNextToken(TokenType.LineStringStart, "\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.StringContent, "hello", "hello");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.InterpolationStart, @"\{");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.LineStringStart, "\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.StringContent, "bye", "bye");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.KeywordVar, "var");
        this.AssertLeadingTrivia((TriviaType.Newline, "\n"));
        this.AssertTrailingTrivia();
        this.AssertDiagnostics();

        this.AssertNextToken(TokenType.CurlyClose, "}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.Identifier, "baz", "baz");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.LineStringStart, "\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.EndOfInput);
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

        this.AssertNextToken(TokenType.MultiLineStringStart, quotes);
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaType.Newline, "\n"));
        this.AssertDiagnostics();

        this.AssertNextToken(TokenType.StringContent, "foo", "foo");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.InterpolationStart, @"\{");
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaType.Newline, "\n"));
        this.AssertDiagnostics();

        this.AssertNextToken(TokenType.Identifier, "x", "x");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.InterpolationEnd, "}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.StringContent, "bar", "bar");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.MultiLineStringEnd, quotes);
        this.AssertLeadingTrivia((TriviaType.Newline, "\n"));
        this.AssertTrailingTrivia();
        this.AssertDiagnostics();

        this.AssertNextToken(TokenType.EndOfInput);
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

        this.AssertNextToken(TokenType.MultiLineStringStart, quotes);
        this.AssertLeadingTrivia();
        this.AssertTrailingTrivia((TriviaType.Newline, "\n"));
        this.AssertDiagnostics();

        this.AssertNextToken(TokenType.StringContent, "foo", "foo");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.InterpolationStart, @"\{");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.LineStringStart, "\"");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.StringContent, "bar", "bar");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.Identifier, "x", "x");
        this.AssertLeadingTrivia((TriviaType.Newline, "\n"));
        this.AssertTrailingTrivia();
        this.AssertDiagnostics();

        this.AssertNextToken(TokenType.InterpolationEnd, "}");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.StringContent, "baz", "baz");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.MultiLineStringEnd, quotes);
        this.AssertLeadingTrivia((TriviaType.Newline, "\n"));
        this.AssertTrailingTrivia();
        this.AssertDiagnostics();

        this.AssertNextToken(TokenType.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
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
        this.Lex(text);

        this.AssertNextToken(tokenType, text, tokenType == TokenType.Identifier ? text : null);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
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
        this.Lex(text);

        this.AssertNextToken(tokenType, text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
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
        this.Lex(text);

        this.AssertNextToken(tokenType, text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
    }

    [Theory]
    [InlineData("0", TokenType.LiteralInteger)]
    [InlineData("123", TokenType.LiteralInteger)]
    [InlineData("12.3", TokenType.LiteralFloat)]
    [Trait("Feature", "Literals")]
    public void TestNumericLiterals(string text, TokenType tokenType)
    {
        this.Lex(text);

        this.AssertNextToken(tokenType, text, text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
    }

    [Theory]
    [InlineData("true", TokenType.KeywordTrue)]
    [InlineData("false", TokenType.KeywordFalse)]
    [Trait("Feature", "Literals")]
    public void TestBoolLiterals(string text, TokenType tokenType)
    {
        this.Lex(text);

        this.AssertNextToken(tokenType, text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
    }

    [Fact]
    [Trait("Feature", "Literals")]
    public void TestIntLiteralWithMethodCall()
    {
        var text = "56.MyFunction()";
        this.Lex(text);

        this.AssertNextToken(TokenType.LiteralInteger, "56", "56");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.Dot, ".");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.Identifier, "MyFunction", "MyFunction");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.ParenOpen, "(");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.ParenClose, ")");
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.EndOfInput);
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

        this.AssertNextToken(TokenType.LiteralCharacter, text, charValue);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken(TokenType.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
    }

    [Fact]
    public void TestUnclosedCharLiteral()
    {
        var text = "'a";
        this.Lex(text);

        this.AssertNextToken(TokenType.LiteralCharacter, text, "a");
        this.AssertNoTrivia();
        this.AssertDiagnostics(SyntaxErrors.UnclosedCharacterLiteral);

        this.AssertNextToken(TokenType.EndOfInput);
        this.AssertNoTriviaOrDiagnostics();
    }

    [Fact]
    public void TestHelloWorld()
    {
        var text = """
            from System.Console import { WriteLine };

            func main() = WriteLine("Hello, World!");
            """;
        this.Lex(text);

        this.AssertNextToken(TokenType.KeywordFrom, "from");
        this.AssertNextToken(TokenType.Identifier, "System", "System");
        this.AssertNextToken(TokenType.Dot, ".");
        this.AssertNextToken(TokenType.Identifier, "Console", "Console");
        this.AssertNextToken(TokenType.KeywordImport, "import");
        this.AssertNextToken(TokenType.CurlyOpen, "{");
        this.AssertNextToken(TokenType.Identifier, "WriteLine", "WriteLine");
        this.AssertNextToken(TokenType.CurlyClose, "}");
        this.AssertNextToken(TokenType.Semicolon, ";");
        this.AssertNextToken(TokenType.KeywordFunc, "func");
        this.AssertNextToken(TokenType.Identifier, "main", "main");
        this.AssertNextToken(TokenType.ParenOpen, "(");
        this.AssertNextToken(TokenType.ParenClose, ")");
        this.AssertNextToken(TokenType.Assign, "=");
        this.AssertNextToken(TokenType.Identifier, "WriteLine", "WriteLine");
        this.AssertNextToken(TokenType.ParenOpen, "(");
        this.AssertNextToken(TokenType.LineStringStart, "\"");
        this.AssertNextToken(TokenType.StringContent, "Hello, World!", "Hello, World!");
        this.AssertNextToken(TokenType.LineStringEnd, "\"");
        this.AssertNextToken(TokenType.ParenClose, ")");
        this.AssertNextToken(TokenType.Semicolon, ";");
        this.AssertNextToken(TokenType.EndOfInput);
    }
}
