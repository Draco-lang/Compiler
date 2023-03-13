using Draco.Compiler.Internal.Syntax;

namespace Draco.Fuzzer;

internal sealed class TokenArray
{
    public IEnumerable<SyntaxToken> Tokens { get; }

    public TokenArray(IEnumerable<SyntaxToken> tokens)
    {
        this.Tokens = tokens;
    }

    public override string ToString() => string.Join("", this.Tokens.Select(x => x.Text));
}
