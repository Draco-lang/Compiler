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
        var currentContext = this.GetCurrentContexts(tree, cursor);
        foreach (var provider in this.providers)
        {
            if (provider.IsApplicableIn(currentContext))
            {
                result.AddRange(provider.GetCompletionItems(tree, semanticModel, cursor, currentContext));
            }
        }
        return result.ToImmutable();
    }

    /// <summary>
    /// Gets current context based on location of <paramref name="cursor"/> in the <paramref name="syntaxTree"/>.
    /// </summary>
    /// <param name="syntaxTree">The <see cref="SyntaxTree"/> in which to find contexts.</param>
    /// <param name="cursor">The location in the <paramref name="syntaxTree"/>.</param>
    /// <returns>Flag enum of the currently valid <see cref="CompletionContext"/>s.</returns>
    private CompletionContext GetCurrentContexts(SyntaxTree syntaxTree, SyntaxPosition cursor)
    {
        var node = syntaxTree.Root.TraverseSubtreesAtCursorPosition(cursor).Last();
        return node switch
        {
            InlineFunctionBodySyntax => CompletionContext.Expression,
            BlockFunctionBodySyntax => CompletionContext.Declaration | CompletionContext.Expression,
            _ => node.Parent switch
            {
                // Type expression
                NameTypeSyntax => CompletionContext.Type,
                // Parameter name declaration
                ParameterSyntax => CompletionContext.None,
                // Global declaration
                UnexpectedDeclarationSyntax => CompletionContext.Declaration,
                // Declaring identifier
                DeclarationSyntax => CompletionContext.None,
                // Member access
                MemberExpressionSyntax => CompletionContext.Expression | CompletionContext.MemberAccess,
                // Member type access
                MemberTypeSyntax => CompletionContext.Type | CompletionContext.MemberAccess,
                // Import member
                MemberImportPathSyntax => CompletionContext.Import | CompletionContext.MemberAccess,
                // Import start
                RootImportPathSyntax => CompletionContext.Import,
                // Global scope
                null => CompletionContext.Declaration,
                // Start of statement inside function
                _ when node.Parent?.Parent is ExpressionStatementSyntax exprStmt => exprStmt.Children.Count() == 2
                    ? CompletionContext.Expression | CompletionContext.Declaration
                    : CompletionContext.Expression,
                _ => CompletionContext.Expression,
            },
        };
    }
}
