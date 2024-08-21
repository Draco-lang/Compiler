using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

/// <summary>
/// Provides keyword completions.
/// </summary>
public sealed class KeywordCompletionProvider : CompletionProvider
{
    private static ImmutableArray<CompletionItem> GetDeclarationKeywords(SourceText source, SourceSpan span) =>
    [
        CompletionItem.Create(source, "import", span, CompletionKind.Keyword),
        CompletionItem.Create(source, "var", span, CompletionKind.Keyword),
        CompletionItem.Create(source, "val", span, CompletionKind.Keyword),
        CompletionItem.Create(source, "func", span, CompletionKind.Keyword),
        CompletionItem.Create(source, "internal", span, CompletionKind.Keyword),
        CompletionItem.Create(source, "public", span, CompletionKind.Keyword),
        CompletionItem.Create(source, "module", span, CompletionKind.Keyword),
    ];

    private static ImmutableArray<CompletionItem> GetExpressionKeywords(SourceText source, SourceSpan span) =>
    [
        CompletionItem.Create(source, "if", span, CompletionKind.Keyword),
        CompletionItem.Create(source, "while", span, CompletionKind.Keyword),
        CompletionItem.Create(source, "for", span, CompletionKind.Keyword),
        CompletionItem.Create(source, "return", span, CompletionKind.Keyword),
        CompletionItem.Create(source, "goto", span, CompletionKind.Keyword),
        CompletionItem.Create(source, "and", span, CompletionKind.Keyword),
        CompletionItem.Create(source, "or", span, CompletionKind.Keyword),
        CompletionItem.Create(source, "not", span, CompletionKind.Keyword),
        CompletionItem.Create(source, "mod", span, CompletionKind.Keyword),
        CompletionItem.Create(source, "rem", span, CompletionKind.Keyword),
    ];

    public override bool IsApplicableIn(CompletionContext context)
    {
        if (context.HasFlag(CompletionContext.Member)) return false;
        return context.HasFlag(CompletionContext.Declaration) || context.HasFlag(CompletionContext.Expression);
    }

    public override ImmutableArray<CompletionItem> GetCompletionItems(
        SyntaxTree tree, SemanticModel semanticModel, int cursorIndex, CompletionContext contexts)
    {
        var cursor = tree.IndexToSyntaxPosition(cursorIndex);
        var syntax = tree.Root.TraverseSubtreesAtCursorPosition(cursor).LastOrDefault();
        if (syntax is null) return [];
        var span = (syntax as SyntaxToken)?.Span ?? new(cursorIndex, 0);
        var result = ImmutableArray.CreateBuilder<CompletionItem>();
        if (contexts.HasFlag(CompletionContext.Expression)) result.AddRange(GetExpressionKeywords(tree.SourceText, span));
        if (contexts.HasFlag(CompletionContext.Declaration)) result.AddRange(GetDeclarationKeywords(tree.SourceText, span));
        return result.ToImmutable();
    }
}
