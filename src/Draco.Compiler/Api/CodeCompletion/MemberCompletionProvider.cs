using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

/// <summary>
/// Provides completions for member access.
/// </summary>
public sealed class MemberCompletionProvider : CompletionProvider
{
    public override bool IsApplicableIn(CompletionContext context)
    {
        if (!context.HasFlag(CompletionContext.Member)) return false;
        return context.HasFlag(CompletionContext.Expression) || context.HasFlag(CompletionContext.Type) || context.HasFlag(CompletionContext.Import);
    }

    public override ImmutableArray<CompletionItem> GetCompletionItems(SyntaxTree tree, SemanticModel semanticModel, SyntaxPosition cursor, CompletionContext contexts)
    {
        var nodesAtCursor = tree.Root.TraverseSubtreesAtCursorPosition(cursor);
        if (nodesAtCursor.LastOrDefault() is not SyntaxToken token) return [];
        var expr = token.Parent;
        var range = token.Kind == TokenKind.Dot ? new SyntaxRange(token.Range.End, 0) : token.Range;
        // If we can't get the accessed propery, we just return empty array
        if (!TryGetMemberAccess(tree, cursor, semanticModel, out var symbols)) return [];
        var completions = symbols
            // NOTE: Not very robust, just like in the other place
            // Also, duplication
            .GroupBy(x => (x.GetType(), x.Name))
            .Select(x => GetCompletionItem([.. x], contexts, range));
        return completions.OfType<CompletionItem>().ToImmutableArray();
    }

    private static bool TryGetMemberAccess(SyntaxTree tree, SyntaxPosition cursor, SemanticModel semanticModel, out ImmutableArray<ISymbol> result)
    {
        var expr = tree.Root.TraverseSubtreesAtCursorPosition(cursor).Last().Parent;
        result = [];
        if (TryDeconstructMemberAccess(expr, out var accessed))
        {
            var referencedType = semanticModel.GetReferencedSymbol(accessed);
            // NOTE: This is how we check for static access
            if (referencedType is ITypeSymbol or IModuleSymbol)
            {
                result = referencedType.StaticMembers.ToImmutableArray();
                return true;
            }
            if (accessed is not ExpressionSyntax accessedExpr) return false;
            var symbol = semanticModel.TypeOf(accessedExpr);
            if (symbol is null) return false;
            result = symbol.InstanceMembers.ToImmutableArray();
            return true;
        }
        return false;
    }

    public static bool TryDeconstructMemberAccess(SyntaxNode? node, [MaybeNullWhen(false)] out SyntaxNode accessed)
    {
        switch (node)
        {
        case MemberExpressionSyntax expr:
            accessed = expr.Accessed;
            return true;
        case MemberTypeSyntax type:
            accessed = type.Accessed;
            return true;
        case MemberImportPathSyntax import:
            accessed = import.Accessed;
            return true;
        default:
            accessed = null;
            return false;
        }
    }

    private static CompletionItem? GetCompletionItem(ImmutableArray<ISymbol> symbols, CompletionContext currentContexts, SyntaxRange range) => symbols.First() switch
    {
        TypeSymbol when currentContexts.HasFlag(CompletionContext.Type)
                     || currentContexts.HasFlag(CompletionContext.Expression) =>
            CompletionItem.Create(symbols.First().Name, range, symbols, CompletionKind.Class),

        ModuleSymbol when currentContexts.HasFlag(CompletionContext.Type)
                       || currentContexts.HasFlag(CompletionContext.Expression)
                       || currentContexts.HasFlag(CompletionContext.Import) =>
            CompletionItem.Create(symbols.First().Name, range, symbols, CompletionKind.Module),

        IVariableSymbol when currentContexts.HasFlag(CompletionContext.Expression) =>
            CompletionItem.Create(symbols.First().Name, range, symbols, CompletionKind.Variable),

        PropertySymbol when currentContexts.HasFlag(CompletionContext.Expression) =>
            CompletionItem.Create(symbols.First().Name, range, symbols, CompletionKind.Property),

        FunctionSymbol fun when !fun.IsSpecialName && currentContexts.HasFlag(CompletionContext.Expression) =>
            CompletionItem.Create(symbols.First().Name, range, symbols, CompletionKind.Function),

        _ => null,
    };
}
