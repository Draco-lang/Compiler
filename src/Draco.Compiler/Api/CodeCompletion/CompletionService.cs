using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

public sealed class CompletionService
{
    public static IList<CompletionItem> GetCompletions(SyntaxTree tree, SemanticModel semanticModel, SyntaxPosition cursor)
    {
        var completions = semanticModel.GetAllDefinedSymbols(tree.Root.TraverseSubtreesAtCursorPosition(cursor).Last()).GroupBy(x => x.Name).Select(x =>
            x.Count() == 1 ? GetCompletionItem(x.First()) : GetOverloadedCompletionItem(x.First(), x.Count()));
        var context = GetContext(tree.Root, cursor);
        var result = new List<CompletionItem>();
        result.AddRange(completions.Where(x => x is not null && x.Contexts.Contains(context))!);
        return result;
    }

    private static CompletionContext GetContext(SyntaxNode node, SyntaxPosition cursor)
    {
        var subtree = node.TraverseSubtreesAtCursorPosition(cursor);
        if (subtree.Last().Parent is NameTypeSyntax) return CompletionContext.TypeReference;
        if (subtree.Any(x => x is FunctionDeclarationSyntax)) return CompletionContext.StatementContent;
        else return CompletionContext.Unknown;
    }

    private static CompletionItem? GetCompletionItem(ISymbol symbol, string? type = null) => symbol switch
    {
        TypeSymbol => new CompletionItem(symbol.Name, CompletionKind.Class, null, symbol.Documentation, CompletionContext.TypeReference),
        LocalSymbol loc =>
            new CompletionItem(symbol.Name, CompletionKind.Variable, type ?? loc.Type.Name, symbol.Documentation, CompletionContext.StatementContent),
        FunctionSymbol fun when !fun.IsSpecialName =>
            new CompletionItem(symbol.Name, CompletionKind.Function, type ?? fun.Type.ToString(), symbol.Documentation, CompletionContext.StatementContent),
        _ => null
    };

    private static CompletionItem? GetOverloadedCompletionItem(ISymbol symbol, int overloadCount) => GetCompletionItem(symbol, $"{overloadCount} overloads");
}
