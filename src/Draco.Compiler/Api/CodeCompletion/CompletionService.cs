using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

public sealed class CompletionService
{
    private List<CompletionProvider> Providers = new List<CompletionProvider>();

    /// <summary>
    /// Adds <see cref="CompletionProvider"/> this service can use.
    /// </summary>
    /// <param name="provider">The provider to add to this service.</param>
    public void AddProvider(CompletionProvider provider) => this.Providers.Add(provider);

    /// <summary>
    /// Gets <see cref="CompletionItem"/>s from all registered <see cref="CompletionProvider"/>s.
    /// </summary>
    /// <param name="tree">The <see cref="SyntaxTree"/> for which this service will create suggestions.</param>
    /// <param name="semanticModel">The <see cref="SemanticModel"/> for this <paramref name="tree"/>.</param>
    /// <param name="cursor">Position of cursor in the <paramref name="tree"/>.</param>
    /// <returns><see cref="CompletionItem"/>s from all <see cref="CompletionProvider"/>s.</returns>
    public ImmutableArray<CompletionItem> GetCompletions(SyntaxTree tree, SemanticModel semanticModel, SyntaxPosition cursor)
    {
        var result = ImmutableArray.CreateBuilder<CompletionItem>();
        foreach (var provider in this.Providers)
        {
            var currentContexts = this.GetCurrentContexts(tree, cursor);
            if (provider.ValidContexts.Intersect(currentContexts).Count() > 0)
            {
                result.AddRange(provider.GetCompletionItems(tree, semanticModel, cursor, currentContexts));
            }
        }
        return result.ToImmutable();
    }

    /// <summary>
    /// Gets current context based on location of <paramref name="cursor"/> in the <paramref name="syntaxTree"/>.
    /// </summary>
    /// <param name="syntaxTree">The <see cref="SyntaxTree"/> in which to find contexts.</param>
    /// <param name="cursor">The location in the <paramref name="syntaxTree"/>.</param>
    /// <returns>Array of the currently valid <see cref="CompletionContext"/>s.</returns>
    private CompletionContext[] GetCurrentContexts(SyntaxTree syntaxTree, SyntaxPosition cursor)
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
