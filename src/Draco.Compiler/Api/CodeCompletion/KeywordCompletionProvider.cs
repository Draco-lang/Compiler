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
    public override CompletionContext ValidContexts { get; } =
        CompletionContext.DeclarationKeyword |
        CompletionContext.Expression;

    private readonly CompletionItem[] declarationKeywords = new[]
    {
        CompletionItem.Create("import", CompletionKind.Keyword),
        CompletionItem.Create("var", CompletionKind.Keyword),
        CompletionItem.Create("val", CompletionKind.Keyword),
        CompletionItem.Create("func", CompletionKind.Keyword)
    };

    private readonly CompletionItem[] expressionKeywords = new[]
    {
        CompletionItem.Create("if", CompletionKind.Keyword),
        CompletionItem.Create("while", CompletionKind.Keyword),
        CompletionItem.Create("return", CompletionKind.Keyword),
        CompletionItem.Create("goto", CompletionKind.Keyword),
        CompletionItem.Create("and", CompletionKind.Keyword),
        CompletionItem.Create("or", CompletionKind.Keyword),
        CompletionItem.Create("not", CompletionKind.Keyword),
        CompletionItem.Create("mod", CompletionKind.Keyword),
        CompletionItem.Create("rem", CompletionKind.Keyword)
    };

    public override ImmutableArray<CompletionItem> GetCompletionItems(SyntaxTree tree, SemanticModel semanticModel, SyntaxPosition cursor, CompletionContext contexts)
    {
        var result = ImmutableArray.CreateBuilder<CompletionItem>();
        if (contexts.HasFlag(CompletionContext.Expression)) result.AddRange(this.expressionKeywords);
        if (contexts.HasFlag(CompletionContext.DeclarationKeyword)) result.AddRange(this.declarationKeywords);
        return result.ToImmutable();
    }
}
