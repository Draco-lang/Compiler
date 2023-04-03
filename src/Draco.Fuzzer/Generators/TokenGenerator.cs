using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Syntax;
using SyntaxToken = Draco.Compiler.Internal.Syntax.SyntaxToken;
using SyntaxTrivia = Draco.Compiler.Internal.Syntax.SyntaxTrivia;

namespace Draco.Fuzzer.Generators;

/// <summary>
/// Generates a random valid token.
/// </summary>
internal sealed class TokenGenerator : IGenerator<SyntaxToken>
{
    private readonly IGenerator<ImmutableArray<SyntaxTrivia>> triviaGenerator = new TriviaGenerator()
        .Sequence(minLength: 0, maxLength: 10);
    private readonly IGenerator<TokenKind> tokenKindGenerator = Generator.EnumMember<TokenKind>();
    private readonly IGenerator<int> intLiteralGenerator = Generator.Integer(0, 1000);
    private readonly IGenerator<double> floatLiteralGenerator = Generator.Float(0.0, 1000.0);
    private readonly IGenerator<char> charLiteralGenerator = Generator.Character();
    private readonly IGenerator<string> newlineGenerator = Generator.Newline();
    private readonly IGenerator<string> stringContentGenerator = Generator.String();
    private readonly IGenerator<string> hashesGenerator = Generator.String("#", minLength: 0, maxLength: 4);
    private readonly IGenerator<string> quotesGenerator = Generator.String("\"", minLength: 1, maxLength: 4);
    private readonly IGenerator<string> identifierGenerator = Generator.String($"{Charsets.AsciiLetters}_")
        .Zip(Generator.String($"{Charsets.AsciiLettersAndDigits}_"))
        .Map(pair => $"{pair.First}{pair.Second}");

    public SyntaxToken NextEpoch()
    {
        var kind = this.tokenKindGenerator.NextEpoch();
        return this.GenerateToken(kind);
    }

    public SyntaxToken NextMutation() => this.NextEpoch();

    public string ToString(SyntaxToken value) => value.Text;

    private SyntaxToken GenerateToken(TokenKind kind)
    {
        var (text, value) = this.GenerateTokenContent(kind);
        var leadingTrivia = this.triviaGenerator.NextEpoch();
        var trailingTrivia = this.triviaGenerator.NextEpoch();
        var builder = new SyntaxToken.Builder();
        builder
            .SetKind(kind)
            .SetText(text)
            .SetValue(value);
        builder.LeadingTrivia.AddRange(leadingTrivia);
        builder.TrailingTrivia.AddRange(trailingTrivia);
        return builder.Build();
    }

    private (string Text, object? Value) GenerateTokenContent(TokenKind kind) => kind switch
    {
        _ when SyntaxFacts.GetTokenText(kind) is string str => (str, null),
        TokenKind.LiteralInteger => this.GenerateLiteralInteger(),
        TokenKind.LiteralFloat => this.GenerateLiteralFloat(),
        TokenKind.LiteralCharacter => this.GenerateLiteralCharacter(),
        TokenKind.StringNewline => this.GenerateStringNewline(),
        TokenKind.LineStringStart => this.GenerateLineStringStart(),
        TokenKind.LineStringEnd => this.GenerateLineStringEnd(),
        TokenKind.MultiLineStringStart => this.GenerateMultilineStringStart(),
        TokenKind.MultiLineStringEnd => this.GenerateMultilineStringEnd(),
        TokenKind.StringContent => this.GenerateStringContent(),
        TokenKind.InterpolationStart => this.GenerateInterpolationStart(),
        TokenKind.InterpolationEnd => this.GenerateInterpolationEnd(),
        TokenKind.Identifier => this.GenerateIdentifier(),
        TokenKind.Unknown => this.GenerateUnknown(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private (string Text, object? Value) GenerateLiteralInteger()
    {
        var value = this.intLiteralGenerator.NextEpoch();
        return (value.ToString(), value);
    }

    private (string Text, object? Value) GenerateLiteralFloat()
    {
        var value = this.floatLiteralGenerator.NextEpoch();
        var str = this.floatLiteralGenerator.ToString(value);
        return (str, value);
    }

    private (string Text, object? Value) GenerateLiteralCharacter()
    {
        // TODO: We are missing escapes, sequences, ...
        var value = this.charLiteralGenerator.NextEpoch();
        return ($"'{value}'", value);
    }

    // TODO: Missing regular newline, continuation, ...
    private (string Text, object? Value) GenerateStringNewline() =>
        (this.newlineGenerator.NextEpoch(), string.Empty);

    private (string Text, object? Value) GenerateMultilineStringStart() =>
        ($"{this.hashesGenerator.NextEpoch()}{this.quotesGenerator.NextEpoch()}", null);

    private (string Text, object? Value) GenerateMultilineStringEnd() =>
        ($"{this.quotesGenerator.NextEpoch()}{this.hashesGenerator.NextEpoch()}", null);

    private (string Text, object? Value) GenerateLineStringStart() =>
        ($"{this.hashesGenerator.NextEpoch()}\"", null);

    private (string Text, object? Value) GenerateLineStringEnd() =>
        ($"\"{this.hashesGenerator.NextEpoch()}", null);

    // TODO: Lazy solution, no escapes
    private (string Text, object? Value) GenerateStringContent()
    {
        var value = this.stringContentGenerator.NextEpoch();
        return (value, value);
    }

    private (string Text, object? Value) GenerateInterpolationStart() =>
        ($"\\{this.hashesGenerator.NextEpoch()}{{", null);

    private (string Text, object? Value) GenerateInterpolationEnd() =>
        ("}", null);

    private (string Text, object? Value) GenerateIdentifier() =>
        (this.identifierGenerator.NextEpoch(), null);

    private (string Text, object? Value) GenerateUnknown() => ("$", null);
}
