using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

/// <summary>
/// Provides semantic completion.
/// </summary>
public sealed class ExpressionCompletionProvider : CompletionProvider
{
    public override CompletionContext ValidContexts =>
          CompletionContext.Expression
        | CompletionContext.Type;

    public override ImmutableArray<CompletionItem> GetCompletionItems(SyntaxTree tree, SemanticModel semanticModel, SyntaxPosition cursor, CompletionContext contexts)
    {
        var symbols = semanticModel.GetAllDefinedSymbols(tree.Root.TraverseSubtreesAtCursorPosition(cursor).Last());
        var completions = symbols.GroupBy(x => (x.GetType(), x.Name)).Select(x => GetCompletionItem(x.ToImmutableArray(), contexts));

        // If the current valid contexts intersect with contexts of given completion, we add it to the result
        return completions.Where(x => x is not null).ToImmutableArray()!;
    }

    private static CompletionItem? GetCompletionItem(ImmutableArray<ISymbol> symbols, CompletionContext currentContexts) => symbols.First() switch
    {
        TypeSymbol when (currentContexts & (CompletionContext.Expression | CompletionContext.Type)) != CompletionContext.None =>
            CompletionItem.Create(symbols.First().Name, symbols, CompletionKind.Class),

        IVariableSymbol when (currentContexts & CompletionContext.Expression) != CompletionContext.None =>
            CompletionItem.Create(symbols.First().Name, symbols, CompletionKind.Variable),

        // We need the type context here for qualified type references
        ModuleSymbol when (currentContexts & (CompletionContext.Expression | CompletionContext.Type)) != CompletionContext.None =>
            CompletionItem.Create(symbols.First().Name, symbols, CompletionKind.Module),

        FunctionSymbol fun when !fun.IsSpecialName && (currentContexts & CompletionContext.Expression) != CompletionContext.None =>
            CompletionItem.Create(symbols.First().Name, symbols, CompletionKind.Function),
        _ => null
    };
}
