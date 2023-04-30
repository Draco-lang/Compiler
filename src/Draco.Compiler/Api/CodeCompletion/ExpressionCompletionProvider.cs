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
        if (context.HasFlag(CompletionContext.MemberAccess)) return false;
        return context.HasFlag(CompletionContext.Expression) || context.HasFlag(CompletionContext.Type) || context.HasFlag(CompletionContext.Import);
    }

    public override ImmutableArray<CompletionItem> GetCompletionItems(SyntaxTree tree, SemanticModel semanticModel, SyntaxPosition cursor, CompletionContext contexts)
    {
        var syntax = tree.Root.TraverseSubtreesAtCursorPosition(cursor).LastOrDefault();
        if (syntax is null) return ImmutableArray<CompletionItem>.Empty;
        var symbols = semanticModel.GetAllDefinedSymbols(syntax);
        var completions = symbols.GroupBy(x => (x.GetType(), x.Name)).Select(x => GetCompletionItem(x.ToImmutableArray(), contexts, syntax.Range));
        return completions.OfType<CompletionItem>().ToImmutableArray();
    }

    private static CompletionItem? GetCompletionItem(ImmutableArray<ISymbol> symbols, CompletionContext currentContexts, SyntaxRange range) => symbols.First() switch
    {
        TypeSymbol when currentContexts.HasFlag(CompletionContext.Expression)
                     || currentContexts.HasFlag(CompletionContext.Type) =>
            CompletionItem.Create(symbols.First().Name, range, symbols, CompletionKind.Class),

        IVariableSymbol when currentContexts.HasFlag(CompletionContext.Expression) =>
            CompletionItem.Create(symbols.First().Name, range, symbols, CompletionKind.Variable),

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
