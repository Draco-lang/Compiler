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
        return token.Parent switch
        {
            // Type expression
            NameTypeSyntax => CompletionContext.Type,
            // Parameter name declaration
            ParameterSyntax => CompletionContext.None,
            // Global declaration
            UnexpectedDeclarationSyntax => CompletionContext.DeclarationKeyword,
            // Declaring identifier
            DeclarationSyntax => CompletionContext.None,
            // Member access
            MemberExpressionSyntax => CompletionContext.MemberExpressionAccess,
            // Member type access
            MemberTypeSyntax => CompletionContext.MemberTypeAccess,
            // Import member
            MemberImportPathSyntax => CompletionContext.MemberModuleImport, // TODO: when aliasing this should be just MemberAccess
            // Import start
            RootImportPathSyntax => CompletionContext.RootModuleImport,
            // Start of statement inside function
            _ when token.Parent?.Parent is ExpressionStatementSyntax =>
                token.Parent.Parent.Children.Count() == 2
                ? CompletionContext.Expression | CompletionContext.DeclarationKeyword
                : CompletionContext.Expression,
            _ => CompletionContext.Expression,
        };
    }
}
