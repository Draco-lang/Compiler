using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

public sealed class KeywordCompletionProvider : CompletionProvider
{
    internal override CompletionContext[] ValidContexts { get; } = new[]
    {
        CompletionContext.DeclarationKeyword,
        CompletionContext.ExpressionContent
    };

    private CompletionItem[] keywords = new[]
    {
        // TODO: else
        // TODO: break and continue labels
        CompletionItem.Create("import", CompletionKind.Keyword, CompletionContext.DeclarationKeyword),
        CompletionItem.Create("var", CompletionKind.Keyword, CompletionContext.DeclarationKeyword),
        CompletionItem.Create("val", CompletionKind.Keyword, CompletionContext.DeclarationKeyword),
        CompletionItem.Create("func", CompletionKind.Keyword, CompletionContext.DeclarationKeyword),

        CompletionItem.Create("if", CompletionKind.Keyword, CompletionContext.ExpressionContent),
        CompletionItem.Create("while", CompletionKind.Keyword, CompletionContext.ExpressionContent),
        CompletionItem.Create("return", CompletionKind.Keyword, CompletionContext.ExpressionContent),
        CompletionItem.Create("goto", CompletionKind.Keyword, CompletionContext.ExpressionContent),
        CompletionItem.Create("and", CompletionKind.Keyword, CompletionContext.ExpressionContent),
        CompletionItem.Create("or", CompletionKind.Keyword, CompletionContext.ExpressionContent),
        CompletionItem.Create("not", CompletionKind.Keyword, CompletionContext.ExpressionContent),
        CompletionItem.Create("mod", CompletionKind.Keyword, CompletionContext.ExpressionContent),
        CompletionItem.Create("rem", CompletionKind.Keyword, CompletionContext.ExpressionContent),
    };

    internal override ImmutableArray<CompletionItem> GetCompletionItems(SyntaxTree tree, SemanticModel semanticModel, SyntaxPosition cursor, CompletionContext[] currentContexts) =>
        this.keywords.Where(x => x.Context.Intersect(currentContexts).Count() > 0).ToImmutableArray();
}
