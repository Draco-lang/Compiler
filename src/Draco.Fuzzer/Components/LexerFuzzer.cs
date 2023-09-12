using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Syntax;
using Draco.Fuzzer.Generators;
using Draco.Trace;

namespace Draco.Fuzzer.Components;

/// <summary>
/// Fuzzes the lexer.
/// </summary>
internal sealed class LexerFuzzer : ComponentFuzzerBase<string>
{
    public LexerFuzzer(IGenerator<string> inputGenerator)
        : base(inputGenerator)
    {
    }

    protected override void NextEpochInternal(string input)
    {
        var lexer = new Lexer(SourceReader.From(input), new SyntaxDiagnosticTable(), tracer: Tracer.Null);
        while (true)
        {
            var token = lexer.Lex();
            if (token.Kind == TokenKind.EndOfInput) break;
        }
    }

    protected override void NextMutationInternal(string oldInput, string newInput) => throw new NotImplementedException();
}
