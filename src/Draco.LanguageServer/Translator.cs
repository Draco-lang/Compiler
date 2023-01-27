using System.Collections.Immutable;
using System.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol;
using CompilerApi = Draco.Compiler.Api;
using LspModels = OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Draco.LanguageServer;

/// <summary>
/// Does translation between Compiler API and LSP types.
/// </summary>
internal static class Translator
{
    public static CompilerApi.Syntax.Position ToCompiler(LspModels.Position position) =>
        new(Line: position.Line, Column: position.Character);

    public static LspModels.Diagnostic ToLsp(CompilerApi.Diagnostics.Diagnostic diag) => new()
    {
        Message = diag.Message,
        // TODO: Not necessarily an error
        Severity = LspModels.DiagnosticSeverity.Error,
        // TODO: Is there a no-range option?
        Range = ToLsp(diag.Location.Range) ?? new(),
        Code = new LspModels.DiagnosticCode(diag.Template.Code),
        RelatedInformation = diag.RelatedInformation
            .Select(ToLsp)
            .OfType<LspModels.DiagnosticRelatedInformation>()
            .ToList(),
    };

    public static LspModels.DiagnosticRelatedInformation? ToLsp(
        CompilerApi.Diagnostics.DiagnosticRelatedInformation info) => ToLsp(info.Location) is { } location
        ? new()
        {
            Location = location,
            Message = info.Message,
        }
        : null;

    public static LspModels.Location? ToLsp(CompilerApi.Diagnostics.Location location) => location.SourceText.Path is null
        ? null
        : new()
        {
            // TODO: Is there a no-range option?
            Range = ToLsp(location.Range) ?? new(),
            Uri = DocumentUri.From(location.SourceText.Path),
        };

    public static LspModels.Range? ToLsp(CompilerApi.Syntax.Range? range) => range is null
        ? null
        : ToLsp(range.Value);

    public static LspModels.Range ToLsp(CompilerApi.Syntax.Range range) =>
        new(ToLsp(range.Start), ToLsp(range.End));

    public static LspModels.Position ToLsp(CompilerApi.Syntax.Position position) =>
        new(line: position.Line, character: position.Column);

    public static SemanticToken? ToLsp(CompilerApi.Syntax.SyntaxToken token) => token.Type switch
    {
        CompilerApi.Syntax.TokenType.LineStringStart
     or CompilerApi.Syntax.TokenType.LineStringEnd
     or CompilerApi.Syntax.TokenType.MultiLineStringStart
     or CompilerApi.Syntax.TokenType.MultiLineStringEnd
     or CompilerApi.Syntax.TokenType.LiteralCharacter =>
        // TODO
        throw new System.NotImplementedException()
        /*new SemanticToken(
            LspModels.SemanticTokenType.String,
            LspModels.SemanticTokenModifier.Defaults.ToImmutableList(),
            token.Range)*/,
        _ => null,
    };
}
