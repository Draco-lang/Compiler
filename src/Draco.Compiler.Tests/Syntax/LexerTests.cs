using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Syntax;
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

    private void AssertNoTrivia()
    {
        Assert.Empty(this.Current.LeadingTrivia);
        Assert.Empty(this.Current.TrailingTrivia);
    }

    private void AssertNoDiagnostics()
    {
        Assert.False(this.diagnostics.TryGetValue(this.Current, out _));
    }

    private Diagnostic AssertSingleDiagnostic()
    {
        Assert.True(this.diagnostics.TryGetValue(this.Current, out var diags));
        Assert.Single(diags);
        return diags![0];
    }

    private void AssertNoTriviaOrDiagnostics()
    {
        this.AssertNoTrivia();
        this.AssertNoDiagnostics();
    }

    private void AssertLeadingTrivia(params string[] trivia)
    {
        Assert.Equal(trivia.Length, this.Current.LeadingTrivia.Length);
        Assert.True(trivia.SequenceEqual(this.Current.LeadingTrivia.Select(t => t.Text)));
    }

    private void AssertTrailingTrivia(params string[] trivia)
    {
        Assert.Equal(trivia.Length, this.Current.TrailingTrivia.Length);
        Assert.True(trivia.SequenceEqual(this.Current.TrailingTrivia.Select(t => t.Text)));
    }

    [Fact]
    [Trait("Feature", "Comments")]
    public void TestLineComment()
    {
        var text = "// Hello, comments";
        this.Lex(text);

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Single(this.Current.LeadingTrivia);
        Assert.Empty(this.Current.TrailingTrivia);
        this.AssertNoDiagnostics();
        Assert.Equal(string.Empty, this.Current.Text);
        Assert.Equal("// Hello, comments", this.Current.LeadingTrivia[0].Text);
        Assert.Equal(TriviaType.LineComment, this.Current.LeadingTrivia[0].Type);
    }

    [Fact]
    [Trait("Feature", "Comments")]
    public void TestMultilineDocumentationComment()
    {
        var text = """
        /// Hello,
        /// multiline doc comments
        """;
        this.Lex(text);

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        // Second trivia is newline
        Assert.Equal(3, this.Current.LeadingTrivia.Length);
        Assert.Empty(this.Current.TrailingTrivia);
        this.AssertNoDiagnostics();
        Assert.Equal(string.Empty, this.Current.Text);
        Assert.Equal("/// Hello,", this.Current.LeadingTrivia[0].Text);
        Assert.Equal("/// multiline doc comments", this.Current.LeadingTrivia[2].Text);
        Assert.Equal(TriviaType.DocumentationComment, this.Current.LeadingTrivia[0].Type);
        Assert.Equal(TriviaType.DocumentationComment, this.Current.LeadingTrivia[2].Type);
    }

    [Fact]
    [Trait("Feature", "Comments")]
    public void TestSinglelineDocumentationComment()
    {
        var text = "/// Hello, doc comments";
        this.Lex(text);

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Single(this.Current.LeadingTrivia);
        Assert.Empty(this.Current.TrailingTrivia);
        this.AssertNoDiagnostics();
        Assert.Equal(string.Empty, this.Current.Text);
        Assert.Equal("/// Hello, doc comments", this.Current.LeadingTrivia[0].Text);
        Assert.Equal(TriviaType.DocumentationComment, this.Current.LeadingTrivia[0].Type);
    }

    [Fact]
    [Trait("Feature", "Strings")]
    public void TestLineString()
    {
        var text = """
            "Hello, line strings!"
            """;
        this.Lex(text);

        this.AssertNextToken();
        Assert.Equal(TokenType.LineStringStart, this.Current.Type);
        Assert.Equal("\"", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal("Hello, line strings!", this.Current.Text);
        Assert.Equal("Hello, line strings!", this.Current.ValueText);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.LineStringEnd, this.Current.Type);
        Assert.Equal("\"", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Equal(string.Empty, this.Current.Text);
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

        this.AssertNextToken();
        Assert.Equal(TokenType.LineStringStart, this.Current.Type);
        Assert.Equal("\"", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal("Hello, line strings!", this.Current.Text);
        Assert.Equal("Hello, line strings!", this.Current.ValueText);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Equal(string.Empty, this.Current.Text);
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

        this.AssertNextToken();
        Assert.Equal(TokenType.LineStringStart, this.Current.Type);
        Assert.Equal("\"", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal("Hello, line strings!", this.Current.Text);
        Assert.Equal("Hello, line strings!", this.Current.ValueText);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Equal(string.Empty, this.Current.Text);
        Assert.Single(this.Current.LeadingTrivia);
        Assert.Equal(TriviaType.Newline, this.Current.LeadingTrivia[0].Type);
        Assert.Equal("\n", this.Current.LeadingTrivia[0].Text);
        Assert.Empty(this.Current.TrailingTrivia);
        this.AssertNoDiagnostics();
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

        this.AssertNextToken();
        Assert.Equal(TokenType.LineStringStart, this.Current.Type);
        Assert.Equal($"{ext}\"", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal(@$"\{ext}""\{ext}\\{ext}n\{ext}'\{ext}u{{1F47D}}\{ext}0", this.Current.Text);
        Assert.Equal("\"\\\n'👽\0", this.Current.ValueText);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.LineStringEnd, this.Current.Type);
        Assert.Equal($"\"{ext}", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Equal(string.Empty, this.Current.Text);
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

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal(@$"\{ext}u{{}}", this.Current.Text);
        Assert.Equal("", this.Current.ValueText);
        this.AssertNoTrivia();
        var diag = this.AssertSingleDiagnostic();
        Assert.Equal(SyntaxErrors.ZeroLengthUnicodeCodepoint, diag.Template);

        this.AssertNextToken();
        Assert.Equal(TokenType.LineStringEnd, this.Current.Type);
        Assert.Equal($"\"{ext}", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Equal(string.Empty, this.Current.Text);
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

        this.AssertNextToken();
        Assert.Equal(TokenType.LineStringStart, this.Current.Type);
        Assert.Equal($"{ext}\"", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal(@$"\{ext}u{{3S}}", this.Current.Text);
        Assert.Equal("S}", this.Current.ValueText); //TODO: change this when we get better orrors out of invalid unicode codepoints
        this.AssertNoTrivia();
        var diag = this.AssertSingleDiagnostic();
        Assert.Equal(SyntaxErrors.UnclosedUnicodeCodepoint, diag.Template);

        this.AssertNextToken();
        Assert.Equal(TokenType.LineStringEnd, this.Current.Type);
        Assert.Equal($"\"{ext}", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Equal(string.Empty, this.Current.Text);
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

        this.AssertNextToken();
        Assert.Equal(TokenType.LineStringStart, this.Current.Type);
        Assert.Equal($"{ext}\"", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal(@$"\{ext}u{{", this.Current.Text);
        Assert.Equal("", this.Current.ValueText);
        this.AssertNoTrivia();
        var diag = this.AssertSingleDiagnostic();
        Assert.Equal(SyntaxErrors.UnclosedUnicodeCodepoint, diag.Template);

        this.AssertNextToken();
        Assert.Equal(TokenType.LineStringEnd, this.Current.Type);
        Assert.Equal($"\"{ext}", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Equal(string.Empty, this.Current.Text);
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

        this.AssertNextToken();
        Assert.Equal(TokenType.LineStringStart, this.Current.Type);
        Assert.Equal("##\"", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal(@"\a\#n\#u{123}\##t", this.Current.Text);
        Assert.Equal("\\a\\#n\\#u{123}\t", this.Current.ValueText);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.LineStringEnd, this.Current.Type);
        Assert.Equal($"\"##", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Equal(string.Empty, this.Current.Text);
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

        this.AssertNextToken();
        Assert.Equal(TokenType.LineStringStart, this.Current.Type);
        Assert.Equal($"{ext}\"", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal(@$"\{ext}y", this.Current.Text);
        Assert.Equal("y", this.Current.ValueText);
        this.AssertNoTrivia();
        var diag = this.AssertSingleDiagnostic();
        Assert.Equal(SyntaxErrors.IllegalEscapeCharacter, diag.Template);

        this.AssertNextToken();
        Assert.Equal(TokenType.LineStringEnd, this.Current.Type);
        Assert.Equal($"\"{ext}", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Equal(string.Empty, this.Current.Text);
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

        this.AssertNextToken();
        Assert.Equal(TokenType.MultiLineStringStart, this.Current.Type);
        Assert.Equal($"{ext}{quotes}", this.Current.Text);
        Assert.Empty(this.Current.LeadingTrivia);
        Assert.Single(this.Current.TrailingTrivia);
        Assert.Equal(TriviaType.Newline, this.Current.TrailingTrivia[0].Type);
        this.AssertNoDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal("    Hello!", this.Current.Text);
        Assert.Equal("    Hello!", this.Current.ValueText);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringNewline, this.Current.Type);
        Assert.Equal("\n", this.Current.Text);
        Assert.Equal("\n", this.Current.ValueText);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal("    Bye!", this.Current.Text);
        Assert.Equal("    Bye!", this.Current.ValueText);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.MultiLineStringEnd, this.Current.Type);
        Assert.Equal($"{quotes}{ext}", this.Current.Text);
        this.AssertLeadingTrivia("\n", "    ");
        Assert.Empty(this.Current.TrailingTrivia);
        this.AssertNoDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Equal(string.Empty, this.Current.Text);
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

        this.AssertNextToken();
        Assert.Equal(TokenType.MultiLineStringStart, this.Current.Type);
        Assert.Equal($"{ext}{quotes}", this.Current.Text);
        Assert.Empty(this.Current.LeadingTrivia);
        this.AssertTrailingTrivia("\n");
        Assert.Equal(TriviaType.Newline, this.Current.TrailingTrivia[0].Type);
        this.AssertNoDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.MultiLineStringEnd, this.Current.Type);
        Assert.Equal($"{quotes}{ext}", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Equal(string.Empty, this.Current.Text);
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

        this.AssertNextToken();
        Assert.Equal(TokenType.MultiLineStringStart, this.Current.Type);
        Assert.Equal($"{ext}{quotes}", this.Current.Text);
        Assert.Empty(this.Current.LeadingTrivia);
        this.AssertTrailingTrivia("\n");
        Assert.Equal(TriviaType.Newline, this.Current.TrailingTrivia[0].Type);
        this.AssertNoDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.MultiLineStringEnd, this.Current.Type);
        Assert.Equal($"{quotes}{ext}", this.Current.Text);
        this.AssertLeadingTrivia("    ");
        Assert.Empty(this.Current.TrailingTrivia);
        this.AssertNoDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Equal(string.Empty, this.Current.Text);
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

        this.AssertNextToken();
        Assert.Equal(TokenType.MultiLineStringStart, this.Current.Type);
        Assert.Equal($"{ext}{quotes}", this.Current.Text);
        Assert.Empty(this.Current.LeadingTrivia);
        this.AssertTrailingTrivia("    ");
        this.AssertNoDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.MultiLineStringEnd, this.Current.Type);
        Assert.Equal($"{quotes}{ext}", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Equal(string.Empty, this.Current.Text);
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

        this.AssertNextToken();
        Assert.Equal(TokenType.MultiLineStringStart, this.Current.Type);
        Assert.Equal($"{ext}{quotes}", this.Current.Text);
        Assert.Empty(this.Current.LeadingTrivia);
        this.AssertTrailingTrivia(" ");
        this.AssertNoDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal($"hello", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.MultiLineStringEnd, this.Current.Type);
        Assert.Equal($"{quotes}{ext}", this.Current.Text);
        this.AssertLeadingTrivia(" ");
        Assert.Empty(this.Current.TrailingTrivia);
        this.AssertNoDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Equal(string.Empty, this.Current.Text);
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

        this.AssertNextToken();
        Assert.Equal(TokenType.MultiLineStringStart, this.Current.Type);
        Assert.Equal($"{ext}{quotes}", this.Current.Text);
        Assert.Empty(this.Current.LeadingTrivia);
        Assert.Single(this.Current.TrailingTrivia);
        Assert.Equal(TriviaType.Newline, this.Current.TrailingTrivia[0].Type);
        this.AssertNoDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal("    Hello!", this.Current.Text);
        Assert.Equal("    Hello!", this.Current.ValueText);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringNewline, this.Current.Type);
        Assert.Equal("\n", this.Current.Text);
        Assert.Equal("\n", this.Current.ValueText);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal("    Bye!", this.Current.Text);
        Assert.Equal("    Bye!", this.Current.ValueText);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.MultiLineStringEnd, this.Current.Type);
        Assert.Equal($"{quotes}{ext}", this.Current.Text);
        this.AssertLeadingTrivia("\n");
        Assert.Empty(this.Current.TrailingTrivia);
        this.AssertNoDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Equal(string.Empty, this.Current.Text);
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

        this.AssertNextToken();
        Assert.Equal(TokenType.MultiLineStringStart, this.Current.Type);
        Assert.Equal($"{ext}{quotes}", this.Current.Text);
        Assert.Empty(this.Current.LeadingTrivia);
        Assert.Single(this.Current.TrailingTrivia);
        Assert.Equal(TriviaType.Newline, this.Current.TrailingTrivia[0].Type);
        this.AssertNoDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal("    Hello!", this.Current.Text);
        Assert.Equal("    Hello!", this.Current.ValueText);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringNewline, this.Current.Type);
        Assert.Equal($"\\{ext}\n", this.Current.Text);
        Assert.Equal(string.Empty, this.Current.ValueText);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal("    Bye!", this.Current.Text);
        Assert.Equal("    Bye!", this.Current.ValueText);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.MultiLineStringEnd, this.Current.Type);
        Assert.Equal($"{quotes}{ext}", this.Current.Text);
        this.AssertLeadingTrivia("\n", "    ");
        Assert.Empty(this.Current.TrailingTrivia);
        this.AssertNoDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Equal(string.Empty, this.Current.Text);
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

        this.AssertNextToken();
        Assert.Equal(TokenType.MultiLineStringStart, this.Current.Type);
        Assert.Equal($"{ext}{quotes}", this.Current.Text);
        Assert.Empty(this.Current.LeadingTrivia);
        Assert.Single(this.Current.TrailingTrivia);
        Assert.Equal(TriviaType.Newline, this.Current.TrailingTrivia[0].Type);
        this.AssertNoDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal("    Hello!", this.Current.Text);
        Assert.Equal("    Hello!", this.Current.ValueText);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringNewline, this.Current.Type);
        Assert.Equal($"\\{ext}{trailingSpace}\n", this.Current.Text);
        Assert.Equal(string.Empty, this.Current.ValueText);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal("    Bye!", this.Current.Text);
        Assert.Equal("    Bye!", this.Current.ValueText);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.MultiLineStringEnd, this.Current.Type);
        Assert.Equal($"{quotes}{ext}", this.Current.Text);
        this.AssertLeadingTrivia("\n", "    ");
        Assert.Empty(this.Current.TrailingTrivia);
        this.AssertNoDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Equal(string.Empty, this.Current.Text);
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

        this.AssertNextToken();
        Assert.Equal(TokenType.LineStringStart, this.Current.Type);
        Assert.Equal($"{ext}\"", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal("x = ", this.Current.Text);
        Assert.Equal("x = ", this.Current.ValueText);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.InterpolationStart, this.Current.Type);
        Assert.Equal($@"\{ext}{{", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.Identifier, this.Current.Type);
        Assert.Equal("x", this.Current.Text);
        Assert.Equal("x", this.Current.ValueText);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.InterpolationEnd, this.Current.Type);
        Assert.Equal("}", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal(", x + y = ", this.Current.Text);
        Assert.Equal(", x + y = ", this.Current.ValueText);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.InterpolationStart, this.Current.Type);
        Assert.Equal($@"\{ext}{{", this.Current.Text);
        Assert.Empty(this.Current.LeadingTrivia);
        this.AssertTrailingTrivia(" ");
        this.AssertNoDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.Identifier, this.Current.Type);
        Assert.Equal("x", this.Current.Text);
        Assert.Equal("x", this.Current.ValueText);
        Assert.Empty(this.Current.LeadingTrivia);
        this.AssertTrailingTrivia(" ");
        this.AssertNoDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.Plus, this.Current.Type);
        Assert.Equal("+", this.Current.Text);
        Assert.Empty(this.Current.LeadingTrivia);
        this.AssertTrailingTrivia(" ");
        this.AssertNoDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.Identifier, this.Current.Type);
        Assert.Equal("y", this.Current.Text);
        Assert.Equal("y", this.Current.ValueText);
        Assert.Empty(this.Current.LeadingTrivia);
        this.AssertTrailingTrivia(" ");
        this.AssertNoDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.InterpolationEnd, this.Current.Type);
        Assert.Equal("}", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal(", y = ", this.Current.Text);
        Assert.Equal(", y = ", this.Current.ValueText);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.InterpolationStart, this.Current.Type);
        Assert.Equal($@"\{ext}{{", this.Current.Text);
        Assert.Empty(this.Current.LeadingTrivia);
        this.AssertTrailingTrivia(" ");
        this.AssertNoDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.CurlyOpen, this.Current.Type);
        Assert.Equal("{", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.Identifier, this.Current.Type);
        Assert.Equal("y", this.Current.Text);
        Assert.Equal("y", this.Current.ValueText);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.CurlyClose, this.Current.Type);
        Assert.Equal("}", this.Current.Text);
        Assert.Empty(this.Current.LeadingTrivia);
        this.AssertTrailingTrivia(" ");
        this.AssertNoDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.InterpolationEnd, this.Current.Type);
        Assert.Equal("}", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.LineStringEnd, this.Current.Type);
        Assert.Equal($"\"{ext}", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Equal(string.Empty, this.Current.Text);
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

        this.AssertNextToken();
        Assert.Equal(TokenType.LineStringStart, this.Current.Type);
        Assert.Equal("\"", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal("hello", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.InterpolationStart, this.Current.Type);
        Assert.Equal(@"\{", this.Current.Text);
        Assert.Empty(this.Current.LeadingTrivia);
        this.AssertTrailingTrivia("\n");
        this.AssertNoDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.KeywordVar, this.Current.Type);
        Assert.Equal("var", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.CurlyClose, this.Current.Type);
        Assert.Equal("}", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.Identifier, this.Current.Type);
        Assert.Equal("bye", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.LineStringStart, this.Current.Type);
        Assert.Equal("\"", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Equal(string.Empty, this.Current.Text);
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

        this.AssertNextToken();
        Assert.Equal(TokenType.LineStringStart, this.Current.Type);
        Assert.Equal("\"", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal("hello", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.InterpolationStart, this.Current.Type);
        Assert.Equal(@"\{", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.LineStringStart, this.Current.Type);
        Assert.Equal("\"", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal("bye", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.KeywordVar, this.Current.Type);
        Assert.Equal("var", this.Current.Text);
        Assert.Single(this.Current.LeadingTrivia);
        Assert.Equal("\n", this.Current.LeadingTrivia[0].Text);
        Assert.Empty(this.Current.TrailingTrivia);
        this.AssertNoDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.CurlyClose, this.Current.Type);
        Assert.Equal("}", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.Identifier, this.Current.Type);
        Assert.Equal("baz", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.LineStringStart, this.Current.Type);
        Assert.Equal("\"", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Equal(string.Empty, this.Current.Text);
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

        this.AssertNextToken();
        Assert.Equal(TokenType.MultiLineStringStart, this.Current.Type);
        Assert.Equal(quotes, this.Current.Text);
        Assert.Empty(this.Current.LeadingTrivia);
        Assert.Single(this.Current.TrailingTrivia);
        Assert.Equal("\n", this.Current.TrailingTrivia[0].Text);
        this.AssertNoDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal("foo", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.InterpolationStart, this.Current.Type);
        Assert.Equal(@"\{", this.Current.Text);
        Assert.Empty(this.Current.LeadingTrivia);
        Assert.Single(this.Current.TrailingTrivia);
        Assert.Equal("\n", this.Current.TrailingTrivia[0].Text);
        this.AssertNoDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.Identifier, this.Current.Type);
        Assert.Equal("x", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.InterpolationEnd, this.Current.Type);
        Assert.Equal("}", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal("bar", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.MultiLineStringEnd, this.Current.Type);
        Assert.Equal(quotes, this.Current.Text);
        Assert.Single(this.Current.LeadingTrivia);
        Assert.Equal("\n", this.Current.LeadingTrivia[0].Text);
        Assert.Empty(this.Current.TrailingTrivia);
        this.AssertNoDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Equal(string.Empty, this.Current.Text);
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

        this.AssertNextToken();
        Assert.Equal(TokenType.MultiLineStringStart, this.Current.Type);
        Assert.Equal(quotes, this.Current.Text);
        Assert.Empty(this.Current.LeadingTrivia);
        Assert.Single(this.Current.TrailingTrivia);
        Assert.Equal("\n", this.Current.TrailingTrivia[0].Text);
        this.AssertNoDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal("foo", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.InterpolationStart, this.Current.Type);
        Assert.Equal(@"\{", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.LineStringStart, this.Current.Type);
        Assert.Equal("\"", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal("bar", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.Identifier, this.Current.Type);
        Assert.Equal("x", this.Current.Text);
        Assert.Single(this.Current.LeadingTrivia);
        Assert.Equal("\n", this.Current.LeadingTrivia[0].Text);
        Assert.Empty(this.Current.TrailingTrivia);
        this.AssertNoDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.InterpolationEnd, this.Current.Type);
        Assert.Equal("}", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal("baz", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.MultiLineStringEnd, this.Current.Type);
        Assert.Equal(quotes, this.Current.Text);
        Assert.Single(this.Current.LeadingTrivia);
        Assert.Equal("\n", this.Current.LeadingTrivia[0].Text);
        Assert.Empty(this.Current.TrailingTrivia);
        this.AssertNoDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Equal(string.Empty, this.Current.Text);
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

        this.AssertNextToken();
        Assert.Equal(tokenType, this.Current.Type);
        Assert.Equal(text, this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Equal(string.Empty, this.Current.Text);
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

        this.AssertNextToken();
        Assert.Equal(tokenType, this.Current.Type);
        Assert.Equal(text, this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Equal(string.Empty, this.Current.Text);
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

        this.AssertNextToken();
        Assert.Equal(tokenType, this.Current.Type);
        Assert.Equal(text, this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Equal(string.Empty, this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();
    }

    [Theory]
    [InlineData("0", TokenType.LiteralInteger)]
    [InlineData("123", TokenType.LiteralInteger)]
    [InlineData("12.3", TokenType.LiteralFloat)]
    [InlineData("true", TokenType.KeywordTrue)]
    [Trait("Feature", "Literals")]
    public void TestLiteral(string text, TokenType tokenType)
    {
        this.Lex(text);

        this.AssertNextToken();
        Assert.Equal(tokenType, this.Current.Type);
        Assert.Equal(text, this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Equal(string.Empty, this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();
    }

    [Fact]
    [Trait("Feature", "Literals")]
    public void TestIntLiteralWithMethodCall()
    {
        string text = "56.MyFunction()";
        this.Lex(text);

        this.AssertNextToken();
        Assert.Equal(TokenType.LiteralInteger, this.Current.Type);
        Assert.Equal("56", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.Dot, this.Current.Type);
        Assert.Equal(".", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.Identifier, this.Current.Type);
        Assert.Equal("MyFunction", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.ParenOpen, this.Current.Type);
        Assert.Equal("(", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.ParenClose, this.Current.Type);
        Assert.Equal(")", this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Equal(string.Empty, this.Current.Text);
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

        this.AssertNextToken();
        Assert.Equal(TokenType.LiteralCharacter, this.Current.Type);
        Assert.Equal(text, this.Current.Text);
        Assert.Equal(charValue, this.Current.ValueText);
        this.AssertNoTriviaOrDiagnostics();

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Equal(string.Empty, this.Current.Text);
        this.AssertNoTriviaOrDiagnostics();
    }

    [Fact]
    public void TestUnclosedCharLiteral()
    {
        string text = "'a";
        this.Lex(text);

        this.AssertNextToken();
        Assert.Equal(TokenType.LiteralCharacter, this.Current.Type);
        Assert.Equal(text, this.Current.Text);
        Assert.Equal("a", this.Current.ValueText);
        this.AssertNoTrivia();
        var diag = this.AssertSingleDiagnostic();
        Assert.Equal(SyntaxErrors.UnclosedCharacterLiteral, diag.Template);

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
        Assert.Equal(string.Empty, this.Current.Text);
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

        this.AssertNextToken();
        Assert.Equal(TokenType.KeywordFrom, this.Current.Type);

        this.AssertNextToken();
        Assert.Equal(TokenType.Identifier, this.Current.Type);
        Assert.Equal("System", this.Current.Text);

        this.AssertNextToken();
        Assert.Equal(TokenType.Dot, this.Current.Type);

        this.AssertNextToken();
        Assert.Equal(TokenType.Identifier, this.Current.Type);
        Assert.Equal("Console", this.Current.Text);

        this.AssertNextToken();
        Assert.Equal(TokenType.KeywordImport, this.Current.Type);

        this.AssertNextToken();
        Assert.Equal(TokenType.CurlyOpen, this.Current.Type);

        this.AssertNextToken();
        Assert.Equal(TokenType.Identifier, this.Current.Type);
        Assert.Equal("WriteLine", this.Current.Text);

        this.AssertNextToken();
        Assert.Equal(TokenType.CurlyClose, this.Current.Type);

        this.AssertNextToken();
        Assert.Equal(TokenType.Semicolon, this.Current.Type);

        this.AssertNextToken();
        Assert.Equal(TokenType.KeywordFunc, this.Current.Type);

        this.AssertNextToken();
        Assert.Equal(TokenType.Identifier, this.Current.Type);
        Assert.Equal("main", this.Current.Text);

        this.AssertNextToken();
        Assert.Equal(TokenType.ParenOpen, this.Current.Type);

        this.AssertNextToken();
        Assert.Equal(TokenType.ParenClose, this.Current.Type);

        this.AssertNextToken();
        Assert.Equal(TokenType.Assign, this.Current.Type);

        this.AssertNextToken();
        Assert.Equal(TokenType.Identifier, this.Current.Type);
        Assert.Equal("WriteLine", this.Current.Text);

        this.AssertNextToken();
        Assert.Equal(TokenType.ParenOpen, this.Current.Type);

        this.AssertNextToken();
        Assert.Equal(TokenType.LineStringStart, this.Current.Type);

        this.AssertNextToken();
        Assert.Equal(TokenType.StringContent, this.Current.Type);
        Assert.Equal("Hello, World!", this.Current.Text);
        Assert.Equal("Hello, World!", this.Current.ValueText);

        this.AssertNextToken();
        Assert.Equal(TokenType.LineStringEnd, this.Current.Type);

        this.AssertNextToken();
        Assert.Equal(TokenType.ParenClose, this.Current.Type);

        this.AssertNextToken();
        Assert.Equal(TokenType.Semicolon, this.Current.Type);

        this.AssertNextToken();
        Assert.Equal(TokenType.EndOfInput, this.Current.Type);
    }
}
