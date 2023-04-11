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
        IEnumerable<CompletionItem?>? completions = null;
        if (TryGetMemberAccess(tree, cursor, semanticModel))
        {
            completions = semanticModel.GetAllDefinedSymbols(tree.Root.TraverseSubtreesAtCursorPosition(cursor).Last()).GroupBy(x => x.Name).Select(x =>
                x.Count() == 1 ? GetCompletionItem(x.First()) : GetOverloadedCompletionItem(x.First(), x.Count()));
        }
        else
        {
            completions = semanticModel.GetAllDefinedSymbols(tree.Root.TraverseSubtreesAtCursorPosition(cursor).Last()).GroupBy(x => x.Name).Select(x =>
                x.Count() == 1 ? GetCompletionItem(x.First()) : GetOverloadedCompletionItem(x.First(), x.Count()));
        }
        var result = new List<CompletionItem>();
        result.AddRange(completions.Where(x => x is not null)!);
        return result;
    }

    private static bool TryGetMemberAccess(SyntaxTree tree, SyntaxPosition cursor, SemanticModel semanticModel)
    {
        var expr = tree.Root.TraverseSubtreesAtCursorPosition(cursor).Last().Parent;
        if (expr is not null
            && expr is MemberExpressionSyntax member)
        {
            var symbol = semanticModel.GetReferencedSymbol(member.Accessed);
            return true;
        }
        return false;
    }

    private static CompletionItem? GetCompletionItem(ISymbol symbol, string? type = null) => symbol switch
    {
        TypeSymbol => new CompletionItem(symbol.Name, CompletionKind.Class, null, symbol.Documentation),

        LocalSymbol loc =>
            new CompletionItem(symbol.Name, CompletionKind.Variable, type ?? loc.Type.Name, symbol.Documentation),

        ModuleSymbol module =>
            new CompletionItem(symbol.Name, CompletionKind.Class, null, symbol.Documentation),

        FunctionSymbol fun when !fun.IsSpecialName =>
            new CompletionItem(symbol.Name, CompletionKind.Function, type ?? fun.Type.ToString(), symbol.Documentation),
        _ => null
    };

    private static CompletionItem? GetOverloadedCompletionItem(ISymbol symbol, int overloadCount) => GetCompletionItem(symbol, $"{overloadCount} overloads");
}
