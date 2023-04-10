using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

public static class CompletionService
{
    private readonly static CompletionItem[] keywords = new CompletionItem[]
    {
        new("func", Context.DeclarationStart, Context.StatementStart),
        new("var", Context.DeclarationStart, Context.StatementStart),
        new("val", Context.DeclarationStart, Context.StatementStart),

        new("goto", Context.StatementStart),
        new("return", Context.StatementStart),
        new("if", Context.StatementStart),
        new("while", Context.StatementStart),

        new("else", Context.ElseBranchStart),
    };

    public static IList<string> GetCompletions(SyntaxTree tree, SyntaxPosition cursor)
    {
        var context = GetContext(tree.Root, cursor);
        var result = new List<string>();
        foreach (var item in keywords.Where(x => x.Contexts.Contains(context)))
        {
            result.Add(item.Text);
        }
        return result;
    }

    private static Context GetContext(SyntaxNode node, SyntaxPosition cursor)
    {
        var subtree = node.TraverseSubtreesAtPosition(cursor);
        //if (subtree.LastOrDefault(x => x is ExpressionStatementSyntax)) return Context.ElseBranchStart;
        if (subtree.Any(x => x is FunctionDeclarationSyntax)) return Context.StatementStart; // TODO: EOF wrong
        else return Context.DeclarationStart;
    }
}
