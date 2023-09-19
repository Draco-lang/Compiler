using Draco.Compiler.Api.Syntax;
using SyntaxTrivia = Draco.Compiler.Internal.Syntax.SyntaxTrivia;

namespace Draco.Fuzzer.Generators;

/// <summary>
/// Generates a random valid syntax trivia.
/// </summary>
internal sealed class TriviaGenerator : IGenerator<SyntaxTrivia>
{
    private readonly IGenerator<TriviaKind> triviaKindGenerator = Generator.EnumMember<TriviaKind>();
    private readonly IGenerator<string> whitespaceGenerator = Generator.String(" \t", minLength: 1, maxLength: 10);
    private readonly IGenerator<string> newlineGenerator = Generator.Newline();
    private readonly IGenerator<string> lineCommentGenerator = Generator.String().Map(x => $"//{x}");
    private readonly IGenerator<string> documentationCommentGenerator = Generator.String().Map(x => $"///{x}");

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
        TriviaKind.LineComment => this.lineCommentGenerator.NextEpoch(),
        TriviaKind.DocumentationComment => this.documentationCommentGenerator.NextEpoch(),
        TriviaKind.Whitespace => this.whitespaceGenerator.NextEpoch(),
        TriviaKind.Newline => this.newlineGenerator.NextEpoch(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
