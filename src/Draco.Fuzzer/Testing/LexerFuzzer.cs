using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Syntax;
using Draco.Fuzzer.Testing.Generators;

namespace Draco.Fuzzer.Testing;

internal sealed class LexerFuzzer : ComponentFuzzer
{
    private readonly IInputGenerator<string> generator;

    public LexerFuzzer(IInputGenerator<string> generator)
    {
        this.generator = generator;
    }

    public override void RunEpoch()
    {
        var input = this.generator.NextExpoch();
        var lexer = new Lexer(SourceReader.From(input));
        try
        {
            while (true)
            {
                var token = lexer.Lex();
                if (token.Type == TokenType.EndOfInput) break;
            }
        }
        catch (Exception ex)
        {
            this.AddError(ex, input);
        }
    }

    public override void RunMutation() => throw new NotImplementedException();
}
