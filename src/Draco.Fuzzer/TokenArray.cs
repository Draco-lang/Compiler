using Draco.Compiler.Internal.Syntax;

namespace Draco.Fuzzer;

internal sealed class TokenArray
{
    private readonly IEnumerable<SyntaxToken> tokens;
    public ReadOnlyMemory<SyntaxToken> Memory { get; }

    public TokenArray(IEnumerable<SyntaxToken> tokens)
    {
        this.tokens = tokens;
        this.Memory = new ReadOnlyMemory<SyntaxToken>(tokens.ToArray());
    }

    public override string ToString() => string.Join("", this.tokens.Select(x => x.Text));
}
