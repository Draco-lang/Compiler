using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

/// <summary>
/// Provides semantic completion.
/// </summary>
public sealed class SemanticCompletionProvider : CompletionProvider
{
    public override CompletionContext[] ValidContexts { get; } = new[]
    {
        CompletionContext.ExpressionContent,
        CompletionContext.MemberAccess,
        CompletionContext.Type,
        CompletionContext.ModuleImport
    };

    public override ImmutableArray<CompletionItem> GetCompletionItems(SyntaxTree tree, SemanticModel semanticModel, SyntaxPosition cursor, CompletionContext[] currentContexts)
    {
        if (!TryGetMemberAccess(tree, cursor, semanticModel, out var symbols))
        {
            symbols = semanticModel.GetAllDefinedSymbols(tree.Root.TraverseSubtreesAtCursorPosition(cursor).Last());
        }
        var completions = symbols.GroupBy(x => (x.GetType(), x.Name)).Select(x => GetCompletionItem(x.ToImmutableArray(), currentContexts));

        // If the current valid contexts intersect with contexts of given completion, we add it to the result
        return completions.Where(x => x is not null).ToImmutableArray()!;
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
    private static CompletionItem? GetCompletionItem(ImmutableArray<ISymbol> symbols, CompletionContext[] currentContexts) => symbols.First() switch
    {
        TypeSymbol when currentContexts.Any(x => x == CompletionContext.ExpressionContent || x == CompletionContext.MemberAccess || x == CompletionContext.Type) =>
            CompletionItem.Create(symbols.First().Name, symbols, CompletionKind.Class),

        IVariableSymbol when currentContexts.Any(x => x == CompletionContext.ExpressionContent) =>
            CompletionItem.Create(symbols.First().Name, symbols, CompletionKind.Variable),

        // We need the type context here for qualified type references
        ModuleSymbol when currentContexts.Any(x => x == CompletionContext.ExpressionContent || x == CompletionContext.MemberAccess || x == CompletionContext.ModuleImport || x == CompletionContext.Type) =>
            CompletionItem.Create(symbols.First().Name, symbols, CompletionKind.Module),

        FunctionSymbol fun when !fun.IsSpecialName && currentContexts.Any(x => x == CompletionContext.ExpressionContent || x == CompletionContext.MemberAccess) =>
            CompletionItem.Create(symbols.First().Name, symbols, CompletionKind.Function),
        _ => null
    };
}
