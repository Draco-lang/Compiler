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
        var triviaKindToGenerate = this.triviaKindGenerator.NextEpoch();
        return this.GenerateTrivia(triviaKindToGenerate);
    }

    public SyntaxTrivia NextMutation() => this.NextEpoch();

    // TODO
    public string ToString(SyntaxTrivia value) => throw new NotImplementedException();

    private SyntaxTrivia GenerateTrivia(TriviaKind kind) => kind switch
    {
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
