using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Syntax;
using Draco.Fuzzer.Generators;

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

    protected override void NextEpochInternal(ImmutableArray<SyntaxToken> input) =>
        new Parser(TokenSource.From(input.AsMemory()), new SyntaxDiagnosticTable()).ParseCompilationUnit();

    protected override void NextMutationInternal(ImmutableArray<SyntaxToken> oldInput, ImmutableArray<SyntaxToken> newInput) =>
        throw new NotImplementedException();
}
