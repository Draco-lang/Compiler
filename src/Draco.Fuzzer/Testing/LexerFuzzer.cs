using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Syntax;
using Draco.Fuzzer.Testing.Generators;

namespace Draco.Fuzzer.Testing;

internal sealed class LexerFuzzer : ComponentFuzzer<string>
{
    public LexerFuzzer(IInputGenerator<string> generator) : base(generator) { }

    public override void RunEpoch(string input)
    {
        var lexer = new Lexer(SourceReader.From(input), new SyntaxDiagnosticTable());
        while (true)
        {
            var token = lexer.Lex();
            if (token.Kind == TokenKind.EndOfInput) break;
        }
    }

    public override void RunMutation() => throw new NotImplementedException();
}
