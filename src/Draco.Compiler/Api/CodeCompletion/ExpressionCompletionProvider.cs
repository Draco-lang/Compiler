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

    public override ImmutableArray<CompletionItem> GetCompletionItems(SyntaxTree tree, SemanticModel semanticModel, SyntaxPosition cursor, CompletionContext contexts)
    {
        var syntax = tree.Root.TraverseSubtreesAtCursorPosition(cursor).LastOrDefault();
        if (syntax is null) return ImmutableArray<CompletionItem>.Empty;
        var symbols = semanticModel.GetAllDefinedSymbols(syntax);
        var range = (syntax as SyntaxToken)?.Range ?? new(cursor, 0);
        var completions = symbols
            // NOTE: Grouping by GetType is very error-prone, maybe we need a symbol "kind"
            .GroupBy(x => (x.GetType(), x.Name))
            .Select(x => GetCompletionItem(x.ToImmutableArray(), contexts, range));
        return completions.OfType<CompletionItem>().ToImmutableArray();
    }

    private static CompletionItem? GetCompletionItem(ImmutableArray<ISymbol> symbols, CompletionContext currentContexts, SyntaxRange range) => symbols.First() switch
    {
        TypeSymbol or TypeAliasSymbol when currentContexts.HasFlag(CompletionContext.Expression)
                                        || currentContexts.HasFlag(CompletionContext.Type) =>
            CompletionItem.Create(symbols.First().Name, range, symbols, CompletionKind.Class),

        IVariableSymbol when currentContexts.HasFlag(CompletionContext.Expression) =>
            CompletionItem.Create(symbols.First().Name, range, symbols, CompletionKind.Variable),

        PropertySymbol when currentContexts.HasFlag(CompletionContext.Expression) =>
            CompletionItem.Create(symbols.First().Name, range, symbols, CompletionKind.Property),

        // We need the type context here for qualified type references
        ModuleSymbol when currentContexts.HasFlag(CompletionContext.Expression)
                       || currentContexts.HasFlag(CompletionContext.Type)
                       || currentContexts.HasFlag(CompletionContext.Import) =>
            CompletionItem.Create(symbols.First().Name, range, symbols, CompletionKind.Module),

        FunctionSymbol fun when !fun.IsSpecialName && currentContexts.HasFlag(CompletionContext.Expression) =>
            CompletionItem.Create(symbols.First().Name, range, symbols, CompletionKind.Function),

        _ => null,
    };
}
