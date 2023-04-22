using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

/// <summary>
/// Provides completions for member access.
/// </summary>
public sealed class MemberAccessCompletionProvider : CompletionProvider
{
    public override CompletionContext ValidContexts =>
          CompletionContext.MemberExpressionAccess
        | CompletionContext.MemberTypeAccess
        | CompletionContext.MemberModuleImport;

    public override ImmutableArray<CompletionItem> GetCompletionItems(SyntaxTree tree, SemanticModel semanticModel, SyntaxPosition cursor, CompletionContext contexts)
    {
        var token = tree.Root.TraverseSubtreesAtCursorPosition(cursor).LastOrDefault();
        if (token is null) return ImmutableArray<CompletionItem>.Empty;
        var expr = token.Parent;
        // If we can't get the accessed propery, we just return empty array
        if (!TryGetMemberAccess(tree, cursor, semanticModel, out var symbols)) return ImmutableArray<CompletionItem>.Empty;
        var completions = symbols.GroupBy(x => (x.GetType(), x.Name)).Select(x => GetCompletionItem(x.ToImmutableArray(), contexts, token.Range));

        // If the current valid contexts intersect with contexts of given completion, we add it to the result
        return completions.Where(x => x is not null).ToImmutableArray()!;
    }

    private static bool TryGetMemberAccess(SyntaxTree tree, SyntaxPosition cursor, SemanticModel semanticModel, out ImmutableArray<ISymbol> result)
    {
        var expr = tree.Root.TraverseSubtreesAtCursorPosition(cursor).Last().Parent;
        result = ImmutableArray<ISymbol>.Empty;
        if (TryDeconstructMemberAccess(expr, out var accessed))
        {
            var symbol = semanticModel.GetReferencedSymbol(accessed);
            if (symbol is null) return false;
            if (symbol is ITypedSymbol typed) result = typed.Type.Members.ToImmutableArray();
            else result = symbol.Members.ToImmutableArray();
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
        TypeSymbol when (currentContexts & (CompletionContext.MemberExpressionAccess | CompletionContext.MemberTypeAccess)) != CompletionContext.None =>
            CompletionItem.Create(symbols.First().Name, range, symbols, CompletionKind.Class),

        // We need the type context here for qualified type references
        ModuleSymbol when (currentContexts & (CompletionContext.MemberExpressionAccess | CompletionContext.MemberTypeAccess | CompletionContext.MemberModuleImport)) != CompletionContext.None =>
            CompletionItem.Create(symbols.First().Name, range, symbols, CompletionKind.Module),

        FunctionSymbol fun when !fun.IsSpecialName && (currentContexts & CompletionContext.MemberExpressionAccess) != CompletionContext.None =>
            CompletionItem.Create(symbols.First().Name, range, symbols, CompletionKind.Function),
        _ => null
    };
}
