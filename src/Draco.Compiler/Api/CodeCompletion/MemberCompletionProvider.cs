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

    public override ImmutableArray<CompletionItem> GetCompletionItems(
        SemanticModel semanticModel, int cursorIndex, CompletionContext contexts)
    {
        var tree = semanticModel.Tree;
        var cursor = tree.IndexToSyntaxPosition(cursorIndex);
        var nodesAtCursor = tree.Root.TraverseSubtreesAtCursorPosition(cursor);
        if (nodesAtCursor.LastOrDefault() is not SyntaxToken token) return [];
        var expr = token.Parent;
        var span = token.Kind == TokenKind.Dot ? new SourceSpan(token.Span.End, 0) : token.Span;
        // If we can't get the accessed propery, we just return empty array
        if (!TryGetMemberAccess(tree, cursor, semanticModel, out var symbols)) return [];
        var completions = symbols
            // NOTE: Not very robust, just like in the other place
            // Also, duplication
            .GroupBy(x => (x.GetType(), x.Name))
            .Select(x => GetCompletionItem(tree.SourceText, [.. x], contexts, span));
        return completions.OfType<CompletionItem>().ToImmutableArray();
    }

    private static bool TryGetMemberAccess(SyntaxTree tree, SyntaxPosition cursor, SemanticModel semanticModel, out ImmutableArray<ISymbol> result)
    {
        // TODO: We disregard visibility here, looking up members we should not
        // Let's introduce some logic that can help us asking for the visible symbols from a given context

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

    private static CompletionItem? GetCompletionItem(
        SourceText source, ImmutableArray<ISymbol> symbols, CompletionContext currentContexts, SourceSpan span) => symbols.First() switch
        {
            ITypeSymbol t when currentContexts.HasFlag(CompletionContext.Type)
                            || currentContexts.HasFlag(CompletionContext.Expression) =>
                CompletionItem.Create(
                    source,
                    t.Name,
                    span,
                    symbols,
                    t.IsValueType ? CompletionKind.ValueTypeName : CompletionKind.ReferenceTypeName),

            IModuleSymbol when currentContexts.HasFlag(CompletionContext.Type)
                            || currentContexts.HasFlag(CompletionContext.Expression)
                            || currentContexts.HasFlag(CompletionContext.Import) =>
                CompletionItem.Create(source, symbols.First().Name, span, symbols, CompletionKind.ModuleName),

            IParameterSymbol when currentContexts.HasFlag(CompletionContext.Expression) =>
                CompletionItem.Create(source, symbols.First().Name, span, symbols, CompletionKind.ParameterName),

            IVariableSymbol when currentContexts.HasFlag(CompletionContext.Expression) =>
                CompletionItem.Create(source, symbols.First().Name, span, symbols, CompletionKind.VariableName),

            IPropertySymbol when currentContexts.HasFlag(CompletionContext.Expression) =>
                CompletionItem.Create(source, symbols.First().Name, span, symbols, CompletionKind.PropertyName),

            IFieldSymbol when currentContexts.HasFlag(CompletionContext.Expression) =>
                CompletionItem.Create(source, symbols.First().Name, span, symbols, CompletionKind.FieldName),

            IFunctionSymbol fun when !fun.IsSpecialName && currentContexts.HasFlag(CompletionContext.Expression) =>
                CompletionItem.Create(source, symbols.First().Name, span, symbols, CompletionKind.FunctionName),

            _ => null,
        };
}
