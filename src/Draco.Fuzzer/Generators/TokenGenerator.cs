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
    private readonly Random random = new();
    private readonly IInputGenerator<string> textGenerator;

    public TokenGenerator(IInputGenerator<string> textGenerator)
    {
        this.textGenerator = textGenerator;
    }

    public SyntaxToken NextExpoch()
    {
        var tokenKindCount = Enum.GetValues(typeof(TokenKind)).Length;
        var tokenKindToGenerate = (TokenKind)this.random.Next(tokenKindCount);
        return this.GenerateToken(tokenKindToGenerate);
    }

    public SyntaxToken NextMutation() => this.NextExpoch();

    private SyntaxToken GenerateToken(TokenKind kind)
    {
        var (text, value) = this.GenerateTokenContent(kind);
        var leadingTrivia = this.GenerateLeadingTrivia();
        var trailingTrivia = this.GenerateTrailingTrivia();
        var builder = new SyntaxToken.Builder();
        builder
            .SetKind(kind)
            .SetText(text)
            .SetValue(value);
        builder.LeadingTrivia.AddRange(leadingTrivia);
        builder.TrailingTrivia.AddRange(trailingTrivia);
        return builder.Build();
    }

    private (string Text, object? Value) GenerateTokenContent(TokenKind kind)
    {
        // TODO
        throw new NotImplementedException();
    }

    private ImmutableArray<SyntaxTrivia> GenerateLeadingTrivia()
    {
        // TODO
        throw new NotImplementedException();
    }

    private ImmutableArray<SyntaxTrivia> GenerateTrailingTrivia()
    {
        // TODO
        throw new NotImplementedException();
    }
}
