using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

public sealed class CompletionService
{
    // TODO: different icons for functions
    public static IList<string> GetCompletions(SyntaxTree tree, SyntaxPosition cursor)
    {
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var completions = semanticModel.GetAllDefinedSymbols(tree.Root.TraverseSubtreesAtCursorPosition(cursor).Last()).Select(x => new CompletionItem(x.Name, GetContext(x)));
        var context = GetContext(tree.Root, cursor);
        var result = new List<string>();
        foreach (var item in completions.Where(x => x.Contexts.Contains(context)))
        {
            result.Add(item.Text);
        }
        return result;
    }

    private static Context GetContext(SyntaxNode node, SyntaxPosition cursor)
    {
        var subtree = node.TraverseSubtreesAtCursorPosition(cursor);
        if (subtree.Any(x => x is FunctionDeclarationSyntax)) return Context.StatementContent;
        else return Context.Unknown;
    }

    private static Context GetContext(ISymbol symbol) => symbol switch
    {
        TypeSymbol => Context.TypeRefeence,
        LocalSymbol => Context.StatementContent,
        FunctionSymbol fun when !fun.IsSpecialName => Context.StatementContent,
        _ => Context.Unknown
    };
}
