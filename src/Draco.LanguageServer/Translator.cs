using System.Collections.Generic;
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

    public static LspModels.CompletionItem ToLsp(CompilerApi.CodeCompletion.CompletionItem item)
    {
        var result = new LspModels.CompletionItem()
        {
            Label = item.Text,
            Kind = ToLsp(item.Kind),
            Detail = item.Type ?? "",
        };
        if (!string.IsNullOrEmpty(item.Documentation)) result.Documentation = new LspModels.MarkupContent()
        {
            Kind = LspModels.MarkupKind.Markdown,
            Value = item.Documentation,
        };
        return result;
    }

    public static LspModels.CompletionItemKind ToLsp(CompilerApi.CodeCompletion.CompletionKind kind) => kind switch
    {
        CompilerApi.CodeCompletion.CompletionKind.Variable => LspModels.CompletionItemKind.Variable,
        CompilerApi.CodeCompletion.CompletionKind.Function => LspModels.CompletionItemKind.Function,
        CompilerApi.CodeCompletion.CompletionKind.Keyword => LspModels.CompletionItemKind.Keyword,
        CompilerApi.CodeCompletion.CompletionKind.Class => LspModels.CompletionItemKind.Class,
        CompilerApi.CodeCompletion.CompletionKind.Module => LspModels.CompletionItemKind.Module,
        _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
    };

    public static LspModels.SignatureHelp? ToLsp(CompilerApi.CodeCompletion.SignatureCollection item) => new()
    {
        Signatures = item.Signatures.Select(x => ToLsp(x)).ToArray(),
        ActiveParameter = item.ActiveParameter is null ? null : (uint)item.ActiveParameter,
        ActiveSignature = (uint)item.ActiveOverload
    };

    public static LspModels.SignatureInformation ToLsp(CompilerApi.CodeCompletion.SignatureItem item)
    {
        var result = new LspModels.SignatureInformation()
        {
            Label = item.Label,
            Parameters = item.Parameters.Select(x => new LspModels.ParameterInformation()
            {
                Label = x,
            }).ToList(),
        };
        if (!string.IsNullOrEmpty(item.Documentation)) result.Documentation = new LspModels.MarkupContent()
        {
            Kind = LspModels.MarkupKind.Markdown,
            Value = item.Documentation,
        };
        return result;
    }
}
