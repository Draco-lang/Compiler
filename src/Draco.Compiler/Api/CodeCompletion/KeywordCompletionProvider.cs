using System;
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
    private static ImmutableArray<CompletionItem> GetDeclarationKeywords(SyntaxRange range) => ImmutableArray.Create(
        CompletionItem.Create("import", range, CompletionKind.Keyword),
        CompletionItem.Create("var", range, CompletionKind.Keyword),
        CompletionItem.Create("val", range, CompletionKind.Keyword),
        CompletionItem.Create("func", range, CompletionKind.Keyword),
        CompletionItem.Create("internal", range, CompletionKind.Keyword),
        CompletionItem.Create("public", range, CompletionKind.Keyword));

    private static ImmutableArray<CompletionItem> GetExpressionKeywords(SyntaxRange range) => ImmutableArray.Create(
        CompletionItem.Create("if", range, CompletionKind.Keyword),
        CompletionItem.Create("while", range, CompletionKind.Keyword),
        CompletionItem.Create("return", range, CompletionKind.Keyword),
        CompletionItem.Create("goto", range, CompletionKind.Keyword),
        CompletionItem.Create("and", range, CompletionKind.Keyword),
        CompletionItem.Create("or", range, CompletionKind.Keyword),
        CompletionItem.Create("not", range, CompletionKind.Keyword),
        CompletionItem.Create("mod", range, CompletionKind.Keyword),
        CompletionItem.Create("rem", range, CompletionKind.Keyword));

    public override bool IsApplicableIn(CompletionContext context)
    {
        if (context.HasFlag(CompletionContext.MemberAccess)) return false;
        return context.HasFlag(CompletionContext.Declaration) || context.HasFlag(CompletionContext.Expression);
    }

    public override ImmutableArray<CompletionItem> GetCompletionItems(SyntaxTree tree, SemanticModel semanticModel, SyntaxPosition cursor, CompletionContext contexts)
    {
        var token = tree.Root.TraverseSubtreesAtCursorPosition(cursor).LastOrDefault();
        if (token is null) return ImmutableArray<CompletionItem>.Empty;
        var result = ImmutableArray.CreateBuilder<CompletionItem>();
        if (contexts.HasFlag(CompletionContext.Expression)) result.AddRange(GetExpressionKeywords(token.Range));
        if (contexts.HasFlag(CompletionContext.Declaration)) result.AddRange(GetDeclarationKeywords(token.Range));
        return result.ToImmutable();
    }
}
