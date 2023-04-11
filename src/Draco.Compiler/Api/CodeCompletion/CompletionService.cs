using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

public sealed class CompletionService
{
    public static IList<CompletionItem> GetCompletions(SyntaxTree tree, SyntaxPosition cursor)
    {
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var completions = semanticModel.GetAllDefinedSymbols(tree.Root.TraverseSubtreesAtCursorPosition(cursor).Last()).DistinctBy(x => x.Name).Select(x => GetCompletionItem(x));
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

    private static CompletionItem? GetCompletionItem(ISymbol symbol) => symbol switch
    {
        TypeSymbol => new CompletionItem(symbol.Name, CompletionKind.Class, CompletionContext.TypeReference),
        LocalSymbol => new CompletionItem(symbol.Name, CompletionKind.Variable, CompletionContext.StatementContent),
        FunctionSymbol fun when !fun.IsSpecialName => new CompletionItem(symbol.Name, CompletionKind.Function, CompletionContext.StatementContent),
        _ => null
    };
}
