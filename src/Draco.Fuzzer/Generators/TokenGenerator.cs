using System.Collections.Immutable;
using System.Text;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Syntax;
using SyntaxToken = Draco.Compiler.Internal.Syntax.SyntaxToken;
using SyntaxTrivia = Draco.Compiler.Internal.Syntax.SyntaxTrivia;

namespace Draco.Fuzzer.Generators;

/// <summary>
/// Generates a random valid token.
/// </summary>
internal sealed class TokenGenerator : IInputGenerator<SyntaxToken>
{
    private readonly IInputGenerator<ImmutableArray<SyntaxTrivia>> triviaGenerator;
    private readonly IInputGenerator<int> intLiteralGenerator;
    private readonly Random random = new();

    public TokenGenerator(
        IInputGenerator<ImmutableArray<SyntaxTrivia>> triviaGenerator,
        IInputGenerator<int> intLiteralGenerator)
    {
        this.triviaGenerator = triviaGenerator;
        this.intLiteralGenerator = intLiteralGenerator;
    }

    public SyntaxToken NextEpoch()
    {
        var tokenKindCount = Enum.GetValues(typeof(TokenKind)).Length;
        var tokenKindToGenerate = (TokenKind)this.random.Next(tokenKindCount);
        return this.GenerateToken(tokenKindToGenerate);
    }

    public SyntaxToken NextMutation() => this.NextEpoch();

    // TODO
    public string ToString(SyntaxToken value) => throw new NotImplementedException();

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
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private (string Text, object? Value) GenerateLiteralInteger()
    {
        var value = this.intLiteralGenerator.NextEpoch();
        return (value.ToString(), value);
    }
}
