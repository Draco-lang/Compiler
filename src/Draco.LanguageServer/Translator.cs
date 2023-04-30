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
        var textEdit = ToLsp(item.Edits[0]);
        var additionalEdits = item.Edits.Skip(1).Select(ToLsp).ToList();

        var detail = string.Empty;
        if (item.Symbols.FirstOrDefault() is CompilerApi.Semantics.ITypedSymbol typed)
        {
            detail = item.Symbols.Length == 1 ? typed.Type.ToString() : $"{item.Symbols.Length} overloads";
        }

        var documentation = new LspModels.MarkupContent();
        if (!string.IsNullOrEmpty(item.Symbols.FirstOrDefault()?.Documentation))
        {
            documentation = new LspModels.MarkupContent()
            {
                Kind = LspModels.MarkupKind.Markdown,
                Value = item.Symbols.First().Documentation,
            };
        }
        return new LspModels.CompletionItem()
        {
            Label = item.DisplayText,
            Kind = ToLsp(item.Kind),
            TextEdit = textEdit,
            AdditionalTextEdits = additionalEdits,
            Detail = detail,
            Documentation = documentation,
        };
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

    public static LspModels.SignatureHelp? ToLsp(CompilerApi.CodeCompletion.SignatureItem? item) => item is null ? null : new()
    {
        Signatures = item.Overloads.Select(x => ToLsp(x)).ToArray(),
        ActiveParameter = item.CurrentParameter is null ? null : (uint)item.CurrentOverload.Parameters.IndexOf(item.CurrentParameter),
        ActiveSignature = (uint)item.Overloads.IndexOf(item.CurrentOverload),
    };

    public static LspModels.SignatureInformation ToLsp(CompilerApi.Semantics.IFunctionSymbol item)
    {
        var documentation = new LspModels.MarkupContent();
        if (!string.IsNullOrEmpty(item.Documentation))
        {
            documentation = new LspModels.MarkupContent()
            {
                Kind = LspModels.MarkupKind.Markdown,
                Value = item.Documentation,
            };
        }
        return new LspModels.SignatureInformation()
        {
            Label = $"func {item.Name}({string.Join(", ", item.Parameters.Select(x => $"{x.Name}: {x.Type}"))}): {item.ReturnType}",
            Parameters = item.Parameters.Select(x => new LspModels.ParameterInformation()
            {
                Label = x.Name,
            }).ToList(),
            Documentation = documentation,
        };
    }

    public static LspModels.TextEdit ToLsp(CompilerApi.TextEdit edit) => new()
    {
        NewText = edit.Text,
        Range = ToLsp(edit.Range),
    };
}
