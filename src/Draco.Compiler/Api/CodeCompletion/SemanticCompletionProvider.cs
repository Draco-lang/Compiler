using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

public sealed class SemanticCompletionProvider : CompletionProvider
{
    internal override CompletionContext[] ValidContexts { get; } = new[]
    {
        CompletionContext.ExpressionContent,
        CompletionContext.MemberAccess,
        CompletionContext.TypeExpression,
        CompletionContext.ModuleImport
    };

    internal override ImmutableArray<CompletionItem> GetCompletionItems(SyntaxTree tree, SemanticModel semanticModel, SyntaxPosition cursor, CompletionContext[] currentContexts)
    {
        if (!TryGetMemberAccess(tree, cursor, semanticModel, out var symbols))
        {
            symbols = semanticModel.GetAllDefinedSymbols(tree.Root.TraverseSubtreesAtCursorPosition(cursor).Last());
        }
        var completions = symbols.GroupBy(x => (x.GetType(), x.Name)).Select(x => GetCompletionItem(x.ToImmutableArray()));

        // If the current valid contexts intersect with contexts of given completion, we add it to the result
        return completions.Where(x => x is not null && x.Context.Intersect(currentContexts).Count() > 0).ToImmutableArray()!;
    }

    private static bool TryGetMemberAccess(SyntaxTree tree, SyntaxPosition cursor, SemanticModel semanticModel, out ImmutableArray<ISymbol> result)
    {
        var expr = tree.Root.TraverseSubtreesAtCursorPosition(cursor).Last().Parent;
        result = ImmutableArray<ISymbol>.Empty;
        if (expr is MemberExpressionSyntax member)
        {
            var symbol = semanticModel.GetReferencedSymbol(member.Accessed);
            if (symbol is null) return false;
            if (symbol is ITypedSymbol typed) result = typed.Type.Members.ToImmutableArray();
            else result = symbol.Members.ToImmutableArray();
            return true;
        }
        else if (expr is MemberImportPathSyntax import)
        {
            var symbol = semanticModel.GetReferencedSymbol(import.Accessed);
            if (symbol is null) return false;
            result = symbol.Members.ToImmutableArray();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Constructs a <see cref="CompletionItem"/> from <see cref="ISymbol"/>.
    /// </summary>
    /// <param name="symbol">The <see cref="ISymbol"/> to construct the <see cref="CompletionItem"/> from.</param>
    /// <param name="type">Optional type, used only for specifying overloads.</param>
    /// <returns>The constructed <see cref="CompletionItem"/>.</returns>
    private static CompletionItem? GetCompletionItem(ImmutableArray<ISymbol> symbols, string? type = null) => symbols.First() switch
    {
        TypeSymbol => CompletionItem.Create(symbols.First().Name, symbols, CompletionKind.Class, CompletionContext.ExpressionContent, CompletionContext.MemberAccess, CompletionContext.TypeExpression),

        IVariableSymbol =>
            CompletionItem.Create(symbols.First().Name, symbols, CompletionKind.Variable, CompletionContext.ExpressionContent),

        ModuleSymbol =>
            CompletionItem.Create(symbols.First().Name, symbols, CompletionKind.Module, CompletionContext.ExpressionContent, CompletionContext.ModuleImport, CompletionContext.MemberAccess),

        FunctionSymbol fun when !fun.IsSpecialName =>
            CompletionItem.Create(symbols.First().Name, symbols, CompletionKind.Function, CompletionContext.ExpressionContent, CompletionContext.MemberAccess),
        _ => null
    };
}
