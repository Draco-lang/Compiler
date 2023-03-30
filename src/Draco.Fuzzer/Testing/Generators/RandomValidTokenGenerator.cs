using System.CommandLine.Help;
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
    private readonly string charset;
    private const string defaultCharset = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~\r\n\t";

    public RandomValidTokenGenerator(int maxLength = 500, string charset = defaultCharset)
    {
        this.random = new Random();
        this.maxLength = maxLength;
        this.charset = charset;
    }

    public RandomValidTokenGenerator(int seed, int maxLength = 500, string charset = defaultCharset) : this(maxLength, charset)
    {
        this.random = new Random(seed);
    }

    public TokenArray NextExpoch()
    {
        var max = this.random.Next(this.maxLength);
        var builder = new StringBuilder();
        for (var i = 0; i < max; i++)
        {
            builder.Append(this.GetRandomToken());
        }

        var lexer = new Lexer(SourceReader.From(builder.ToString()), new SyntaxDiagnosticTable());
        return new TokenArray(this.LexTokens(lexer));
    }

    public TokenArray NextMutation() => throw new NotImplementedException();

    private IEnumerable<SyntaxToken> LexTokens(Lexer lexer)
    {
        var tokens = new List<SyntaxToken>();
        while (true)
        {
            var tok = lexer.Lex();
            tokens.Add(tok);
            if (tok.Kind == TokenKind.EndOfInput) break;
        }
        return tokens;
    }

    private string GetRandomToken()
    {
        var max = Enum.GetValues(typeof(TokenKind)).Length;
        var rand = this.random.Next(max);
        var text = this.GenerateTokenText((TokenKind)rand);
        var toAppend = this.random.Next(2) == 0 ? "" : " "; //Append space or not
        return $"{text}{toAppend}";
    }

    private string GenerateTokenText(TokenKind kind) => kind switch
    {
        TokenKind.Identifier => this.GenerateValidIdentifier(),
        TokenKind.LiteralInteger => this.GenerateInteger(),
        TokenKind.LiteralFloat => this.GenerateFloat(),
        TokenKind.LiteralCharacter => this.GenerateChar(),
        TokenKind.LineStringStart => this.GenerateLineStringStart(),
        TokenKind.LineStringEnd => this.GenerateLineStringEnd(),
        TokenKind.MultiLineStringStart => this.GenerateMultiLineStringStart(),
        TokenKind.MultiLineStringEnd => this.GenerateMultiLineStringEnd(),
        TokenKind.StringContent => this.GenerateStringContent(),
        TokenKind.StringNewline => Environment.NewLine,
        TokenKind.InterpolationStart => this.GenerateInterpolationStart(),
        _ => SyntaxFacts.GetTokenText(kind)!
    };

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

    private string GenerateInteger() => this.random.Next().ToString();

    private string GenerateFloat() => this.random.NextDouble().ToString();

    private string GenerateChar() => $"'{this.charset[this.random.Next(this.charset.Length)]}'";

    private string GenerateDelimiters() => new string('#', this.random.Next(4));

    private string GenerateLineStringStart() => $"{this.GenerateDelimiters()}\"";

    private string GenerateLineStringEnd() => $"\"{this.GenerateDelimiters()}";

    private string GenerateMultiLineStringStart() => $"{this.GenerateDelimiters()}\"\"\"";

    private string GenerateMultiLineStringEnd() => $"\"\"\"{this.GenerateDelimiters()}";

    private string GenerateStringContent() => new string(Enumerable.Range(0, this.random.Next(128)).Select(x => this.charset[this.random.Next(this.charset.Length)]).ToArray());

    private string GenerateInterpolationStart() => $"\\{this.GenerateDelimiters()}{{";
}
