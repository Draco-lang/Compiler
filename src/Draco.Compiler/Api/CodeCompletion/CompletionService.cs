using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

public sealed class CompletionService
{
    private static CompletionItem[] keywords = new[]
    {
        // TODO: else
        // TODO: break and continue labels
        new CompletionItem("var", CompletionKind.Keyword, null, null, CompletionContext.DeclarationKeyword),
        new CompletionItem("val", CompletionKind.Keyword, null, null, CompletionContext.DeclarationKeyword),
        new CompletionItem("func", CompletionKind.Keyword, null, null, CompletionContext.DeclarationKeyword),

        new CompletionItem("if", CompletionKind.Keyword, null, null, CompletionContext.ExpressionContent),
        new CompletionItem("while", CompletionKind.Keyword, null, null, CompletionContext.ExpressionContent),
        new CompletionItem("return", CompletionKind.Keyword, null, null, CompletionContext.ExpressionContent),
        new CompletionItem("goto", CompletionKind.Keyword, null, null, CompletionContext.ExpressionContent),
        new CompletionItem("and", CompletionKind.Keyword, null, null, CompletionContext.ExpressionContent),
        new CompletionItem("or", CompletionKind.Keyword, null, null, CompletionContext.ExpressionContent),
        new CompletionItem("not", CompletionKind.Keyword, null, null, CompletionContext.ExpressionContent),
        new CompletionItem("mod", CompletionKind.Keyword, null, null, CompletionContext.ExpressionContent),
        new CompletionItem("rem", CompletionKind.Keyword, null, null, CompletionContext.ExpressionContent),
    };
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
            completions = semanticModel.GetAllDefinedSymbols(tree.Root.TraverseSubtreesAtCursorPosition(cursor).Last()).GroupBy(x => (x.GetType(), x.Name)).Select(x =>
                x.Count() == 1 ? GetCompletionItem(x.First()) : GetOverloadedCompletionItem(x.First(), x.Count()));
        }
        var contexts = GetContexts(tree, cursor);
        var result = new List<CompletionItem>();
        result.AddRange(keywords.Where(x => contexts.Contains(x.Context)));
        result.AddRange(completions.Where(x => x is not null && contexts.Contains(x.Context))!);
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

    private static IList<CompletionContext> GetContexts(SyntaxTree tree, SyntaxPosition cursor)
    {
        // TODO: function param names
        var token = tree.Root.TraverseSubtreesAtCursorPosition(cursor).Last();
        // Global declaration
        if (token.Parent is UnexpectedDeclarationSyntax declaration) return new[] { CompletionContext.DeclarationKeyword };
        // Declaring identifier
        if (token.Parent is DeclarationSyntax) return new List<CompletionContext>();
        // Start of statement in function
        else if (token.Parent?.Parent is ExpressionStatementSyntax)
        {
            var result = new List<CompletionContext>() { CompletionContext.ExpressionContent };
            // Only one token (second is expected semicolon), we can suggest declaration start
            if (token.Parent.Parent.Children.Count() == 2) result.Add(CompletionContext.DeclarationKeyword);
            return result;
        }
        return new[] { CompletionContext.ExpressionContent };
    }

    private static CompletionItem? GetCompletionItem(ISymbol symbol, string? type = null) => symbol switch
    {
        TypeSymbol => new CompletionItem(symbol.Name, CompletionKind.Class, null, symbol.Documentation, CompletionContext.ExpressionContent),

        LocalSymbol loc =>
            new CompletionItem(symbol.Name, CompletionKind.Variable, type ?? loc.Type.Name, symbol.Documentation, CompletionContext.ExpressionContent),

        ModuleSymbol module =>
            new CompletionItem(symbol.Name, CompletionKind.Class, null, symbol.Documentation, CompletionContext.ExpressionContent),

        FunctionSymbol fun when !fun.IsSpecialName =>
            new CompletionItem(symbol.Name, CompletionKind.Function, type ?? fun.Type.ToString(), symbol.Documentation, CompletionContext.ExpressionContent),
        _ => null
    };

    private static CompletionItem? GetOverloadedCompletionItem(ISymbol symbol, int overloadCount) => GetCompletionItem(symbol, $"{overloadCount} overloads");
}
