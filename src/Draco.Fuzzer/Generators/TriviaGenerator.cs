using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Syntax;
using SyntaxTrivia = Draco.Compiler.Internal.Syntax.SyntaxTrivia;

namespace Draco.Fuzzer.Generators;

/// <summary>
/// Generates a random valid syntax trivia.
/// </summary>
internal sealed class TriviaGenerator : IGenerator<SyntaxTrivia>
{
    private readonly IGenerator<TriviaKind> triviaKindGenerator = Generator.EnumMember<TriviaKind>();

    public SyntaxTrivia NextEpoch()
    {
        var kind = this.triviaKindGenerator.NextEpoch();
        var text = this.GenerateTriviaText(kind);
        return SyntaxTrivia.From(kind, text);
    }

    public SyntaxTrivia NextMutation() => this.NextEpoch();

    // TODO
    public string ToString(SyntaxTrivia value) => throw new NotImplementedException();

    private string GenerateTriviaText(TriviaKind kind) => kind switch
    {
        TriviaKind.LineComment => throw new NotImplementedException(),
        TriviaKind.DocumentationComment => throw new NotImplementedException(),
        TriviaKind.Whitespace => throw new NotImplementedException(),
        TriviaKind.Newline => throw new NotImplementedException(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
