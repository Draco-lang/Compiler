using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Fuzzer.Generators;
using Draco.Compiler.Internal.Syntax;

namespace Draco.Compiler.Fuzzer.Components;

/// <summary>
/// Fuzzes the lexer.
/// </summary>
internal sealed class LexerFuzzer(IGenerator<string> inputGenerator)
    : ComponentFuzzerBase<string>(inputGenerator)
{
    protected override void NextEpochInternal(string input)
    {
        var lexer = new Lexer(SourceReader.From(input), new SyntaxDiagnosticTable());
        while (true)
        {
            var token = lexer.Lex();
            if (token.Kind == TokenKind.EndOfInput) break;
        }
    }

    protected override void NextMutationInternal(string oldInput, string newInput) => throw new NotImplementedException();
}
