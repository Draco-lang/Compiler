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
        var contexts = GetCurrentContexts(tree, cursor);
        var result = ImmutableArray.CreateBuilder<CompletionItem>();

        // If the current valid contexts intersect with contexts of given completion, we add it to the result
        result.AddRange(keywords.Where(x => x.Context.Intersect(contexts).Count() > 0));
        result.AddRange(completions.Where(x => x is not null && x.Context.Intersect(contexts).Count() > 0)!);
        return result.ToImmutable();
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
    /// Gets current context based on location of <paramref name="cursor"/> in the <paramref name="syntaxTree"/>.
    /// </summary>
    /// <param name="syntaxTree">The <see cref="SyntaxTree"/> in which to find contexts.</param>
    /// <param name="cursor">The location in the <paramref name="syntaxTree"/>.</param>
    /// <returns>Array of the currently valid <see cref="CompletionContext"/>s.</returns>
    private static CompletionContext[] GetCurrentContexts(SyntaxTree syntaxTree, SyntaxPosition cursor)
    {
        var token = syntaxTree.Root.TraverseSubtreesAtCursorPosition(cursor).Last();
        // Type expression
        if (token.Parent is NameTypeSyntax) return new[] { CompletionContext.TypeExpression };
        // Parameter name declaration
        if (token.Parent is ParameterSyntax) return new CompletionContext[0];
        // Global declaration
        if (token.Parent is UnexpectedDeclarationSyntax declaration) return new[] { CompletionContext.DeclarationKeyword };
        // Declaring identifier
        if (token.Parent is DeclarationSyntax) return new CompletionContext[0];
        // Member access
        else if (token.Parent is MemberExpressionSyntax) return new[] { CompletionContext.MemberAccess };
        // Import start
        else if (token.Parent is ImportPathSyntax) return new[] { CompletionContext.ModuleImport }; // TODO: when aliasing this should be just MemberAccess
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

    /// <summary>
    /// Constructs a <see cref="CompletionItem"/> from <see cref="ISymbol"/>.
    /// </summary>
    /// <param name="symbol">The <see cref="ISymbol"/> to construct the <see cref="CompletionItem"/> from.</param>
    /// <param name="type">Optional type, used only for specifying overloads.</param>
    /// <returns>The constructed <see cref="CompletionItem"/>.</returns>
    private static CompletionItem? GetCompletionItem(ISymbol symbol, string? type = null) => symbol switch
    {
        TypeSymbol => new CompletionItem(symbol.Name, CompletionKind.Class, null, symbol.Documentation, CompletionContext.ExpressionContent, CompletionContext.MemberAccess, CompletionContext.TypeExpression),

        LocalSymbol loc =>
            new CompletionItem(symbol.Name, CompletionKind.Variable, type ?? loc.Type.Name, symbol.Documentation, CompletionContext.ExpressionContent),

        ModuleSymbol module =>
            new CompletionItem(symbol.Name, CompletionKind.Module, null, symbol.Documentation, CompletionContext.ExpressionContent, CompletionContext.ModuleImport, CompletionContext.MemberAccess),

        FunctionSymbol fun when !fun.IsSpecialName =>
            new CompletionItem(symbol.Name, CompletionKind.Function, type ?? fun.Type.ToString(), symbol.Documentation, CompletionContext.ExpressionContent, CompletionContext.MemberAccess),
        _ => null
    };

    /// <summary>
    /// Constructs a <see cref="CompletionItem"/> from <see cref="ISymbol"/> and specifies type as <paramref name="overloadCount"/>.
    /// </summary>
    /// <param name="symbol">The <see cref="ISymbol"/> to construct the <see cref="CompletionItem"/> from.</param>
    /// <param name="overloadCount">The count of overloads this function has.</param>
    /// <returns>The constructed <see cref="CompletionItem"/>.</returns>
    private static CompletionItem? GetOverloadedCompletionItem(ISymbol symbol, int overloadCount) => GetCompletionItem(symbol, $"{overloadCount} overloads");
}
