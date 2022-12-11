using System.Collections.Immutable;
using System.Linq;
using CompilerApi = Draco.Compiler.Api;
using LspModels = OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Draco.LanguageServer;

/// <summary>
/// Does translation between Compiler API and LSP types.
/// </summary>
internal static class Translator
{
    public static LspModels.Diagnostic ToLsp(CompilerApi.Diagnostics.Diagnostic diag) => new()
    {
        Message = diag.Message,
        // TODO: Not necessarily an error
        Severity = LspModels.DiagnosticSeverity.Error,
        // TODO: Is there a no-range option?
        Range = ToLsp(diag.Location.Range) ?? new(),
        RelatedInformation = diag.RelatedInformation
            .Select(ToLsp)
            .ToList(),
    };

    public static LspModels.DiagnosticRelatedInformation ToLsp(CompilerApi.Diagnostics.DiagnosticRelatedInformation relatedInformation) => new()
    {
        // TODO: Wrong
        Location = new LspModels.Location
        {
            // TODO: Hardcoded
            Uri = OmniSharp.Extensions.LanguageServer.Protocol.DocumentUri.FromFileSystemPath(@"C:/Development/Experiments/Untitled-1.draco"),
            // TODO: Is there a no-range option?
            Range = ToLsp(relatedInformation.Location.Range) ?? new(),
        },
        Message = relatedInformation.Message,
    };

    public static LspModels.Range? ToLsp(CompilerApi.Syntax.Range? range) => range is null
        ? null
        : ToLsp(range.Value);

    public static LspModels.Range ToLsp(CompilerApi.Syntax.Range range) =>
        new(ToLsp(range.Start), ToLsp(range.End));

    public static LspModels.Position ToLsp(CompilerApi.Syntax.Position position) =>
        new(line: position.Line, character: position.Column);

    public static SemanticToken? ToLsp(CompilerApi.Syntax.ParseTree.Token token) => token.Type switch
    {
        CompilerApi.Syntax.TokenType.LineStringStart
     or CompilerApi.Syntax.TokenType.LineStringEnd
     or CompilerApi.Syntax.TokenType.MultiLineStringStart
     or CompilerApi.Syntax.TokenType.MultiLineStringEnd
     or CompilerApi.Syntax.TokenType.LiteralCharacter => new SemanticToken(
            LspModels.SemanticTokenType.String,
            LspModels.SemanticTokenModifier.Defaults.ToImmutableList(),
            token.Range),
        _ => null,
    };
}
