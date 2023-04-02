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
internal sealed class TokenGenerator : IGenerator<SyntaxToken>
{
    private readonly IGenerator<ImmutableArray<SyntaxTrivia>> triviaGenerator = new TriviaGenerator()
        .Sequence(minLength: 0, maxLength: 10);
    private readonly IGenerator<TokenKind> tokenKindGenerator = Generator.EnumMember<TokenKind>();
    private readonly IGenerator<int> intLiteralGenerator = Generator.Integer(0, 1000);

    public SyntaxToken NextEpoch()
    {
        var tokenKindToGenerate = this.tokenKindGenerator.NextEpoch();
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
