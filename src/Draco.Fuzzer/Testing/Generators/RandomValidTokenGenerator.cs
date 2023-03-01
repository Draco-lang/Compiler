using System.CommandLine.Parsing;
using System.Text;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Syntax;
using SyntaxToken = Draco.Compiler.Internal.Syntax.SyntaxToken;

namespace Draco.Fuzzer.Testing.Generators;

internal sealed class RandomValidTokenGenerator : IInputGenerator<TokenArray>
{
    private readonly Random random;
    private readonly int maxLength;

    public RandomValidTokenGenerator(int maxLength = 500)
    {
        this.random = new Random();
        this.maxLength = maxLength;
    }

    public RandomValidTokenGenerator(int seed, int maxLength = 500) : this(maxLength)
    {
        this.random = new Random(seed);
    }

    public TokenArray NextExpoch()
    {
        var max = this.random.Next(this.maxLength);
        var builder = new StringBuilder();
        for (int i = 0; i < max; i++)
        {
            builder.Append(this.GetRandomToken());
        }

        var lexer = new Lexer(SourceReader.From(builder.ToString()), new SyntaxDiagnosticTable());
        return new TokenArray(this.LexTokens(lexer));
    }

    public TokenArray NextMutation() => throw new NotImplementedException();

    private IEnumerable<SyntaxToken> LexTokens(Lexer lexer)
    {
        while (true)
        {
            var tok = lexer.Lex();
            yield return tok;
            if (tok.Kind == TokenKind.EndOfInput) break;
        }
    }

    private string GetRandomToken()
    {
        var max = Enum.GetValues(typeof(TokenKind)).Length;
        var rand = this.random.Next(max);
        var text = SyntaxFacts.GetTokenText((TokenKind)rand);
        if (text == null) text = this.GenerateValidIdentifier();
        var toAppend = this.random.Next(2) == 0 ? "" : " "; //Append space or not
        return $"{text}{toAppend}";
    }

    private string GenerateValidIdentifier()
    {
        var lowerBound = (int)'a';
        var upperBound = ((int)'z' + 1);
        var builder = new StringBuilder();
        builder.Append((char)this.random.Next(lowerBound, upperBound)); // Valid alpha char
        var len = this.random.Next(25);
        for (var i = 0; i < len; i++)
        {
            builder.Append((char)this.random.Next(lowerBound, upperBound));
        }
        return builder.ToString();
    }
}
