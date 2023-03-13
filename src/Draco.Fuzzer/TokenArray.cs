using Draco.Compiler.Internal.Syntax;

namespace Draco.Fuzzer;

internal sealed class TokenArray
{
    private readonly IEnumerable<SyntaxToken> tokens;
    public ReadOnlyMemory<SyntaxToken> Memory => new ReadOnlyMemory<SyntaxToken>(this.tokens.ToArray());

    public TokenArray(IEnumerable<SyntaxToken> tokens)
    {
        this.tokens = tokens;
    }

    public override string ToString() => string.Join("", this.tokens.Select(x => x.Text));
}
