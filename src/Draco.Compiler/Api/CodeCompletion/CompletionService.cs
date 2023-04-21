using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

/// <summary>
/// Manages <see cref="CompletionProvider"/>s and allows to get <see cref="CompletionItem"/>s correctly based on context.
/// </summary>
public sealed class CompletionService
{
    private readonly List<CompletionProvider> providers = new();

    /// <summary>
    /// Adds <see cref="CompletionProvider"/> this service can use.
    /// </summary>
    /// <param name="provider">The provider to add to this service.</param>
    public void AddProvider(CompletionProvider provider) => this.providers.Add(provider);

    /// <summary>
    /// Gets <see cref="CompletionItem"/>s from all applicable <see cref="CompletionProvider"/>s.
    /// </summary>
    /// <param name="tree">The <see cref="SyntaxTree"/> for which this service will create suggestions.</param>
    /// <param name="semanticModel">The <see cref="SemanticModel"/> for this <paramref name="tree"/>.</param>
    /// <param name="cursor">Position of cursor in the <paramref name="tree"/>.</param>
    /// <returns><see cref="CompletionItem"/>s from all <see cref="CompletionProvider"/>s.</returns>
    public ImmutableArray<CompletionItem> GetCompletions(SyntaxTree tree, SemanticModel semanticModel, SyntaxPosition cursor)
    {
        var result = ImmutableArray.CreateBuilder<CompletionItem>();
        var currentContexts = this.GetCurrentContexts(tree, cursor);
        foreach (var provider in this.providers)
        {
            if ((provider.ValidContexts & currentContexts) != CompletionContext.None)
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
    private CompletionContext GetCurrentContexts(SyntaxTree syntaxTree, SyntaxPosition cursor)
    {
        var token = syntaxTree.Root.TraverseSubtreesAtCursorPosition(cursor).Last();
        // Type expression
        if (token.Parent is NameTypeSyntax) return CompletionContext.Type;
        // Parameter name declaration
        else if (token.Parent is ParameterSyntax) return CompletionContext.None;
        // Global declaration
        else if (token.Parent is UnexpectedDeclarationSyntax declaration) return CompletionContext.DeclarationKeyword;
        // Declaring identifier
        else if (token.Parent is DeclarationSyntax) return CompletionContext.None;
        // Member access
        else if (token.Parent is MemberExpressionSyntax) return CompletionContext.MemberExpressionAccess;
        // Member type access
        else if (token.Parent is MemberTypeSyntax) return CompletionContext.MemberTypeAccess;
        // Import start
        else if (token.Parent is ImportPathSyntax) return CompletionContext.ModuleImport; // TODO: when aliasing this should be just MemberAccess
        // Start of statement inside function
        else if (token.Parent?.Parent is ExpressionStatementSyntax)
        {
            // Only one token (second is expected semicolon), we can suggest declaration start
            if (token.Parent.Parent.Children.Count() == 2) return CompletionContext.Expression | CompletionContext.DeclarationKeyword;
        }
        return CompletionContext.Expression;
    }
}
