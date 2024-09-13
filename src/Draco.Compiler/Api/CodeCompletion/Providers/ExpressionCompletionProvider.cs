using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion.Providers;

/// <summary>
/// Provides completion for expressions excluding member access.
/// </summary>
public sealed class ExpressionCompletionProvider : CompletionProvider
{
    public override bool IsApplicableIn(CompletionContext context)
    {
        if (context.HasFlag(CompletionContext.Member)) return false;
        return context.HasFlag(CompletionContext.Expression)
            || context.HasFlag(CompletionContext.Type)
            || context.HasFlag(CompletionContext.Import);
    }

    public override ImmutableArray<CompletionItem> GetCompletionItems(
        SemanticModel semanticModel, int cursorIndex, SyntaxNode? nodeAtCursor, CompletionContext contexts)
    {
        var span = (nodeAtCursor as SyntaxToken)?.Span ?? SourceSpan.Empty(cursorIndex);
        var symbols = semanticModel.GetAllAccessibleSymbols(nodeAtCursor);
        return symbols
            .Select(s => CompletionItem.Simple(span, s))
            .ToImmutableArray();
    }
}
