using Draco.Compiler.Internal.Syntax;

namespace Draco.Fuzzer;

internal class TokenArray
{
    private readonly IEnumerable<SyntaxToken> tokens;

    public IEnumerable<SyntaxToken> GetTokens() => this.tokens;

    public TokenArray(IEnumerable<SyntaxToken> tokens)
    {
        this.tokens = tokens;
    }

    public override string ToString() => string.Join("", this.tokens.Select(x => x.Text));
}
