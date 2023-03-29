using System.Collections.Immutable;
using System.Linq;
using CompilerApi = Draco.Compiler.Api;
using LspModels = Draco.Lsp.Model;

namespace Draco.LanguageServer;

/// <summary>
/// Does translation between Compiler API and LSP types.
/// </summary>
internal static class Translator
{
    public static CompilerApi.Syntax.SyntaxPosition ToCompiler(LspModels.Position position) =>
        new(Line: (int)position.Line, Column: (int)position.Character);

    public static CompilerApi.Syntax.SyntaxRange ToCompiler(LspModels.Range range) =>
        new(Start: ToCompiler(range.Start), End: ToCompiler(range.End));

    public static LspModels.Diagnostic ToLsp(CompilerApi.Diagnostics.Diagnostic diag) => new()
    {
        Message = diag.Message,
        // TODO: Not necessarily an error
        Severity = LspModels.DiagnosticSeverity.Error,
        // TODO: Is there a no-range option?
        Range = ToLsp(diag.Location.Range) ?? new(),
        Code = diag.Template.Code,
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
            Uri = LspModels.DocumentUri.From(location.SourceText.Path),
        };

    public static LspModels.Range? ToLsp(CompilerApi.Syntax.SyntaxRange? range) => range is null
        ? null
        : ToLsp(range.Value);

    public static LspModels.Range ToLsp(CompilerApi.Syntax.SyntaxRange range) => new()
    {
        Start = ToLsp(range.Start),
        End = ToLsp(range.End),
    };

    public static LspModels.Position ToLsp(CompilerApi.Syntax.SyntaxPosition position) => new()
    {
        Line = (uint)position.Line,
        Character = (uint)position.Column,
    };
}
