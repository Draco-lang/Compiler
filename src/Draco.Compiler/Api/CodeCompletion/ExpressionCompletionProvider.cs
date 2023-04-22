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
        | CompletionContext.Type
        | CompletionContext.RootModuleImport;

    public override ImmutableArray<CompletionItem> GetCompletionItems(SyntaxTree tree, SemanticModel semanticModel, SyntaxPosition cursor, CompletionContext contexts)
    {
        var syntax = tree.Root.TraverseSubtreesAtCursorPosition(cursor).LastOrDefault();
        if (syntax is null) return ImmutableArray<CompletionItem>.Empty;
        var symbols = semanticModel.GetAllDefinedSymbols(syntax);
        var completions = symbols.GroupBy(x => (x.GetType(), x.Name)).Select(x => GetCompletionItem(x.ToImmutableArray(), contexts, syntax.Range));

        // If the current valid contexts intersect with contexts of given completion, we add it to the result
        return completions.OfType<CompletionItem>().ToImmutableArray();
    }

    private static CompletionItem? GetCompletionItem(ImmutableArray<ISymbol> symbols, CompletionContext currentContexts, SyntaxRange range) => symbols.First() switch
    {
        TypeSymbol when (currentContexts & (CompletionContext.Expression | CompletionContext.Type)) != CompletionContext.None =>
            CompletionItem.Create(symbols.First().Name, range, symbols, CompletionKind.Class),

        IVariableSymbol when (currentContexts & CompletionContext.Expression) != CompletionContext.None =>
            CompletionItem.Create(symbols.First().Name, range, symbols, CompletionKind.Variable),

        // We need the type context here for qualified type references
        ModuleSymbol when (currentContexts & (CompletionContext.Expression | CompletionContext.Type | CompletionContext.RootModuleImport)) != CompletionContext.None =>
            CompletionItem.Create(symbols.First().Name, range, symbols, CompletionKind.Module),

        FunctionSymbol fun when !fun.IsSpecialName && (currentContexts & CompletionContext.Expression) != CompletionContext.None =>
            CompletionItem.Create(symbols.First().Name, range, symbols, CompletionKind.Function),
        _ => null
    };
}
