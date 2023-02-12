using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Syntax;
using Draco.Fuzzer.Testing.Generators;

namespace Draco.Fuzzer.Testing;

internal class LexerFuzzer : ComponentTester
{
    private IInputGenerator generator;
    public LexerFuzzer(FuzzType fuzzType)
    {
        switch (fuzzType)
        {
        case FuzzType.RandomText: this.generator = new RandomTextGenerator(); break;
        default: throw new NotImplementedException();
        }
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
            Helper.PrintError(ex, input);
        }
    }
    public override void RunMutation() => throw new NotImplementedException();
}
