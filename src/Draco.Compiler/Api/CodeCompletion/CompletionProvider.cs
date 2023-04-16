using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

public abstract class CompletionProvider
{
    /// <summary>
    /// Gets all <see cref="CompletionItem"/>s from this <see cref="CompletionProvider"/>.
    /// </summary>
    /// <param name="tree">The <see cref="SyntaxTree"/> for which this service will create suggestions.</param>
    /// <param name="semanticModel">The <see cref="SemanticModel"/> for this <paramref name="tree"/>.</param>
    /// <param name="cursor">Position of cursor in the <paramref name="tree"/>.</param>
    /// <returns>All the <see cref="CompletionItem"/>s this <see cref="CompletionProvider"/> created.</returns>
    internal abstract ImmutableArray<CompletionItem> GetCompletionItems(SyntaxTree tree, SemanticModel semanticModel, SyntaxPosition cursor);

    /// <summary>
    /// Gets current context based on location of <paramref name="cursor"/> in the <paramref name="syntaxTree"/>.
    /// </summary>
    /// <param name="syntaxTree">The <see cref="SyntaxTree"/> in which to find contexts.</param>
    /// <param name="cursor">The location in the <paramref name="syntaxTree"/>.</param>
    /// <returns>Array of the currently valid <see cref="CompletionContext"/>s.</returns>
    protected CompletionContext[] GetCurrentContexts(SyntaxTree syntaxTree, SyntaxPosition cursor)
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
}
