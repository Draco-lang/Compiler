using System.Text;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Syntax;
using static Draco.Compiler.Internal.Syntax.ParseNode;

namespace Draco.Fuzzer.Testing.Generators;

internal sealed class RandomValidTokenGenerator : IInputGenerator<IEnumerable<Token>>
{
    private Token[]? currentEpoch;
    private readonly Random random;
    private readonly int maxLength;

    public RandomValidTokenGenerator(int maxLength = 500)
    {
        this.random = new Random();
        this.maxLength = maxLength;
    }

    public RandomValidTokenGenerator(int seed, int maxLength = 500)
    {
        this.random = new Random(seed);
        this.maxLength = maxLength;
    }

    public IEnumerable<Token> NextExpoch()
    {
        var max = this.random.Next(this.maxLength);
        var builder = new StringBuilder();
        for (int i = 0; i < max; i++)
        {
            builder.Append(this.GetRandomToken());
        }

        var lexer = new Lexer(SourceReader.From(builder.ToString()));

        while (true)
        {
            var tok = lexer.Lex();
            yield return tok;
            if (tok.Type == TokenType.EndOfInput) break;
        }
    }

    public IEnumerable<Token> NextMutation() => throw new NotImplementedException();

    private string GetRandomToken()
    {
        var max = Enum.GetValues(typeof(TokenType)).Length;
        var rand = this.random.Next(max);
        var text = TokenTypeExtensions.GetTokenTextOrNull((TokenType)rand);
        if (text == null) text = this.GenerateValidIndent();
        var toAppend = this.random.Next(2) == 0 ? "" : " "; //Append space or not
        return $"{text}{toAppend}";
    }

    private string GenerateValidIndent()
    {
        var builder = new StringBuilder();
        builder.Append((char)this.random.Next(97, 123)); // Valid alpha char
        var len = this.random.Next(25);
        for (int i = 0; i < len; i++)
        {
            builder.Append((char)this.random.Next(97, 123));
        }
        return builder.ToString();
    }
}
