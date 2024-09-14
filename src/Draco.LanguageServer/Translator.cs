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
        Severity = diag.Severity switch
        {
            CompilerApi.Diagnostics.DiagnosticSeverity.Info => LspModels.DiagnosticSeverity.Information,
            CompilerApi.Diagnostics.DiagnosticSeverity.Warning => LspModels.DiagnosticSeverity.Warning,
            CompilerApi.Diagnostics.DiagnosticSeverity.Error => LspModels.DiagnosticSeverity.Error,
            _ => throw new System.Exception()
        },
        // TODO: Is there a no-range option?
        Range = ToLsp(diag.Location.Range) ?? default!,
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
            Range = ToLsp(location.Range) ?? default!,
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

    public static LspModels.CompletionItem ToLsp(
        CompilerApi.Syntax.SourceText sourceText,
        CompilerApi.Services.CodeCompletion.CompletionItem item)
    {
        var textEdit = ToLsp(sourceText, item.Edits[0]);
        var additionalEdits = item.Edits
            .Skip(1)
            .Select(s => ToLsp(sourceText, s))
            .ToList();

        LspModels.MarkupContent? documentation = null;
        if (!string.IsNullOrWhiteSpace(item.Symbol?.Documentation))
        {
            documentation = new LspModels.MarkupContent()
            {
                Kind = LspModels.MarkupKind.Markdown,
                Value = item.Symbol.Documentation,
            };
        }

        return new LspModels.CompletionItem()
        {
            Label = item.DisplayText,
            Kind = ToLsp(item.Kind),
            TextEdit = new(textEdit),
            AdditionalTextEdits = additionalEdits,
            Detail = item.DetailsText,
            Documentation = documentation is not null ? new(documentation) : default,
        };
    }

    public static LspModels.CompletionItemKind ToLsp(CompilerApi.Services.CodeCompletion.CompletionKind kind) => kind switch
    {
        CompilerApi.Services.CodeCompletion.CompletionKind.ControlFlowKeyword
     or CompilerApi.Services.CodeCompletion.CompletionKind.DeclarationKeyword
     or CompilerApi.Services.CodeCompletion.CompletionKind.VisibilityKeyword => LspModels.CompletionItemKind.Keyword,

        CompilerApi.Services.CodeCompletion.CompletionKind.VariableName
     or CompilerApi.Services.CodeCompletion.CompletionKind.ParameterName => LspModels.CompletionItemKind.Variable,

        CompilerApi.Services.CodeCompletion.CompletionKind.ModuleName => LspModels.CompletionItemKind.Module,

        CompilerApi.Services.CodeCompletion.CompletionKind.FunctionName => LspModels.CompletionItemKind.Method,

        CompilerApi.Services.CodeCompletion.CompletionKind.PropertyName => LspModels.CompletionItemKind.Property,

        CompilerApi.Services.CodeCompletion.CompletionKind.FieldName => LspModels.CompletionItemKind.Field,

        CompilerApi.Services.CodeCompletion.CompletionKind.ReferenceTypeName => LspModels.CompletionItemKind.Class,
        CompilerApi.Services.CodeCompletion.CompletionKind.ValueTypeName => LspModels.CompletionItemKind.Struct,

        CompilerApi.Services.CodeCompletion.CompletionKind.Operator => LspModels.CompletionItemKind.Operator,

        _ => LspModels.CompletionItemKind.Text,
    };

    public static LspModels.SignatureHelp? ToLsp(CompilerApi.Services.Signature.SignatureItem? item) => item is null ? null : new()
    {
        Signatures = item.Overloads.Select(ToLsp).ToArray(),
        ActiveSignature = (uint)item.Overloads.IndexOf(item.BestMatch),
        ActiveParameter = item.CurrentParameter is null
            ? null
            : (uint)item.BestMatch.Parameters.IndexOf(item.CurrentParameter),
    };

    public static LspModels.SignatureInformation ToLsp(CompilerApi.Semantics.IFunctionSymbol item)
    {
        LspModels.MarkupContent? documentation = null;
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
            Documentation = documentation is not null ? new(documentation) : default,
        };
    }

    public static LspModels.ITextEdit ToLsp(
        CompilerApi.Syntax.SourceText sourceText,
        CompilerApi.Syntax.TextEdit edit) => new LspModels.TextEdit()
        {
            NewText = edit.Text,
            Range = ToLsp(sourceText.SourceSpanToSyntaxRange(edit.Span)),
        };
}
