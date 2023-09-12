using System.Collections.Immutable;
using Draco.Compiler.Internal.Syntax;
using Draco.Fuzzer.Generators;
using Draco.Trace;

namespace Draco.Fuzzer.Components;

/// <summary>
/// Fuzzes the parser.
/// </summary>
internal sealed class ParserFuzzer : ComponentFuzzerBase<ImmutableArray<SyntaxToken>>
{
    public ParserFuzzer(IGenerator<ImmutableArray<SyntaxToken>> inputGenerator)
        : base(inputGenerator)
    {
    }

    protected override void NextEpochInternal(ImmutableArray<SyntaxToken> input)
    {
        // NOTE: To not have to implement every single parsing constraint for the token generator,
        // we stringify the tokens and re-lex them
        var diags = new SyntaxDiagnosticTable();
        var source = string.Join(string.Empty, input.Select(t => t.ToCode()));
        var lexer = new Lexer(SourceReader.From(source), diags, tracer: Tracer.Null);
        new Parser(TokenSource.From(lexer), diags, tracer: Tracer.Null).ParseCompilationUnit();
    }

    protected override void NextMutationInternal(ImmutableArray<SyntaxToken> oldInput, ImmutableArray<SyntaxToken> newInput) =>
        throw new NotImplementedException();
}
