using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion.Providers;

/// <summary>
/// Provides keyword completions.
/// </summary>
public sealed class KeywordCompletionProvider : CompletionProvider
{
    private static ImmutableArray<CompletionItem> GetDeclarationKeywords(SourceSpan span) =>
    [
        CompletionItem.Simple(span, "import", CompletionKind.DeclarationKeyword),
        CompletionItem.Simple(span, "var", CompletionKind.DeclarationKeyword),
        CompletionItem.Simple(span, "val", CompletionKind.DeclarationKeyword),
        CompletionItem.Simple(span, "func", CompletionKind.DeclarationKeyword),
        CompletionItem.Simple(span, "module", CompletionKind.DeclarationKeyword),

        CompletionItem.Simple(span, "internal", CompletionKind.VisibilityKeyword),
        CompletionItem.Simple(span, "public", CompletionKind.VisibilityKeyword),
    ];

    private static ImmutableArray<CompletionItem> GetExpressionKeywords(SourceSpan span) =>
    [
        CompletionItem.Simple(span, "if", CompletionKind.ControlFlowKeyword),
        CompletionItem.Simple(span, "else", CompletionKind.ControlFlowKeyword),
        CompletionItem.Simple(span, "while", CompletionKind.ControlFlowKeyword),
        CompletionItem.Simple(span, "for", CompletionKind.ControlFlowKeyword),
        CompletionItem.Simple(span, "return", CompletionKind.ControlFlowKeyword),
        CompletionItem.Simple(span, "goto", CompletionKind.ControlFlowKeyword),

        CompletionItem.Simple(span, "and", CompletionKind.Operator),
        CompletionItem.Simple(span, "or", CompletionKind.Operator),
        CompletionItem.Simple(span, "not", CompletionKind.Operator),
        CompletionItem.Simple(span, "mod", CompletionKind.Operator),
        CompletionItem.Simple(span, "rem", CompletionKind.Operator),
    ];

    public override bool IsApplicableIn(CompletionContext context)
    {
        if (context.HasFlag(CompletionContext.Member)) return false;
        return context.HasFlag(CompletionContext.Declaration) || context.HasFlag(CompletionContext.Expression);
    }

    public override ImmutableArray<CompletionItem> GetCompletionItems(
        SemanticModel semanticModel, int cursorIndex, SyntaxNode? nodeAtCursor, CompletionContext contexts)
    {
        if (nodeAtCursor is not SyntaxToken token) return [];
        var result = ImmutableArray.CreateBuilder<CompletionItem>();
        if (contexts.HasFlag(CompletionContext.Expression)) result.AddRange(GetExpressionKeywords(token.Span));
        if (contexts.HasFlag(CompletionContext.Declaration)) result.AddRange(GetDeclarationKeywords(token.Span));
        return result.ToImmutable();
    }
}
