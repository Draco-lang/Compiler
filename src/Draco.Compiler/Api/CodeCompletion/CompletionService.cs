using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

public static class CompletionService
{
    private static CompletionItem[] keywords = new[]
    {
        // TODO: else
        // TODO: break and continue labels
        new CompletionItem("import", CompletionKind.Keyword, null, null, CompletionContext.DeclarationKeyword),
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
    public static ImmutableArray<CompletionItem> GetCompletions(SyntaxTree tree, SemanticModel semanticModel, SyntaxPosition cursor)
    {
        if (!TryGetMemberAccess(tree, cursor, semanticModel, out var symbols))
        {
            symbols = semanticModel.GetAllDefinedSymbols(tree.Root.TraverseSubtreesAtCursorPosition(cursor).Last());
        }
        var completions = symbols.GroupBy(x => (x.GetType(), x.Name)).Select(x =>
                x.Count() == 1 ? GetCompletionItem(x.First()) : GetOverloadedCompletionItem(x.First(), x.Count()));
        var contexts = GeturrentContexts(tree, cursor);
        var result = ImmutableArray.CreateBuilder<CompletionItem>();
        result.AddRange(keywords.Where(x => x.Context.Intersect(contexts).Count() > 0));
        result.AddRange(completions.Where(x => x is not null && x.Context.Intersect(contexts).Count() > 0)!);
        return result.ToImmutable();
    }

    private static bool TryGetMemberAccess(SyntaxTree tree, SyntaxPosition cursor, SemanticModel semanticModel, out ImmutableArray<ISymbol> result)
    {
        var expr = tree.Root.TraverseSubtreesAtCursorPosition(cursor).Last().Parent;
        result = ImmutableArray<ISymbol>.Empty;
        if (expr is not null
            && expr is MemberExpressionSyntax member)
        {
            var symbol = semanticModel.GetReferencedSymbol(member.Accessed);
            if (symbol is null) return false;
            if (symbol is ITypedSymbol typeSymbol) result = typeSymbol.Type.Members.ToImmutableArray();
            else result = symbol.Members.ToImmutableArray();
            return true;
        }
        return false;
    }

    private static CompletionContext[] GeturrentContexts(SyntaxTree tree, SyntaxPosition cursor)
    {
        var token = tree.Root.TraverseSubtreesAtCursorPosition(cursor).Last();
        if (token.Parent is NameTypeSyntax) return new[] { CompletionContext.TypeExpression };
        if (token.Parent is ParameterSyntax) return new CompletionContext[0];
        // Global declaration
        if (token.Parent is UnexpectedDeclarationSyntax declaration) return new[] { CompletionContext.DeclarationKeyword };
        // Declaring identifier
        if (token.Parent is DeclarationSyntax) return new CompletionContext[0];
        // Member access
        else if (token.Parent is MemberExpressionSyntax) return new[] { CompletionContext.MemberAccess };
        // Start of statement inside function
        else if (token.Parent?.Parent is ExpressionStatementSyntax)
        {
            var result = new List<CompletionContext>() { CompletionContext.ExpressionContent };
            // Only one token (second is expected semicolon), we can suggest declaration start
            if (token.Parent.Parent.Children.Count() == 2) result.Add(CompletionContext.DeclarationKeyword);
            return result.ToArray();
        }
        return new[] { CompletionContext.ExpressionContent };
    }

    private static CompletionItem? GetCompletionItem(ISymbol symbol, string? type = null) => symbol switch
    {
        TypeSymbol => new CompletionItem(symbol.Name, CompletionKind.Class, null, symbol.Documentation, CompletionContext.ExpressionContent, CompletionContext.MemberAccess, CompletionContext.TypeExpression),

        LocalSymbol loc =>
            new CompletionItem(symbol.Name, CompletionKind.Variable, type ?? loc.Type.Name, symbol.Documentation, CompletionContext.ExpressionContent),

        ModuleSymbol module =>
            new CompletionItem(symbol.Name, CompletionKind.Class, null, symbol.Documentation, CompletionContext.ExpressionContent, CompletionContext.MemberAccess),

        FunctionSymbol fun when !fun.IsSpecialName =>
            new CompletionItem(symbol.Name, CompletionKind.Function, type ?? fun.Type.ToString(), symbol.Documentation, CompletionContext.ExpressionContent, CompletionContext.MemberAccess),
        _ => null
    };

    private static CompletionItem? GetOverloadedCompletionItem(ISymbol symbol, int overloadCount) => GetCompletionItem(symbol, $"{overloadCount} overloads");
}
