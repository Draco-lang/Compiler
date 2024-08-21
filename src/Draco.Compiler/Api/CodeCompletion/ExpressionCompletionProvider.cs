using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

/// <summary>
/// Provides completion for expressions excluding member access.
/// </summary>
public sealed class ExpressionCompletionProvider : CompletionProvider
{
    public override bool IsApplicableIn(CompletionContext context)
    {
        if (context.HasFlag(CompletionContext.Member)) return false;
        return context.HasFlag(CompletionContext.Expression) || context.HasFlag(CompletionContext.Type) || context.HasFlag(CompletionContext.Import);
    }

    public override ImmutableArray<CompletionItem> GetCompletionItems(
        SyntaxTree tree, SemanticModel semanticModel, int cursorIndex, CompletionContext contexts)
    {
        var cursor = tree.IndexToSyntaxPosition(cursorIndex);
        var syntax = tree.Root.TraverseSubtreesAtCursorPosition(cursor).LastOrDefault();
        if (syntax is null) return [];
        var symbols = semanticModel.GetAllDefinedSymbols(syntax);
        var span = (syntax as SyntaxToken)?.Span ?? new(cursorIndex, 0);
        var completions = symbols
            // NOTE: Grouping by GetType is very error-prone, maybe we need a symbol "kind"
            .GroupBy(x => (x.GetType(), x.Name))
            .Select(x => GetCompletionItem(tree.SourceText, [.. x], contexts, span));
        return completions.OfType<CompletionItem>().ToImmutableArray();
    }

    private static CompletionItem? GetCompletionItem(
        SourceText source, ImmutableArray<ISymbol> symbols, CompletionContext currentContexts, SourceSpan span) => symbols.First() switch
        {
            ITypeSymbol or IAliasSymbol when currentContexts.HasFlag(CompletionContext.Expression)
                                          || currentContexts.HasFlag(CompletionContext.Type) =>
                CompletionItem.Create(source, symbols.First().Name, span, symbols, CompletionKind.Class),

            IVariableSymbol when currentContexts.HasFlag(CompletionContext.Expression) =>
                CompletionItem.Create(source, symbols.First().Name, span, symbols, CompletionKind.Variable),

            IPropertySymbol when currentContexts.HasFlag(CompletionContext.Expression) =>
                CompletionItem.Create(source, symbols.First().Name, span, symbols, CompletionKind.Property),

            // We need the type context here for qualified type references
            IModuleSymbol when currentContexts.HasFlag(CompletionContext.Expression)
                           || currentContexts.HasFlag(CompletionContext.Type)
                           || currentContexts.HasFlag(CompletionContext.Import) =>
                CompletionItem.Create(source, symbols.First().Name, span, symbols, CompletionKind.Module),

            IFunctionSymbol fun when !fun.IsSpecialName && currentContexts.HasFlag(CompletionContext.Expression) =>
                CompletionItem.Create(source, symbols.First().Name, span, symbols, CompletionKind.Function),

            _ => null,
        };
}
