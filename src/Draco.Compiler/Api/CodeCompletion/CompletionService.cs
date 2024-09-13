using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.CodeCompletion.Providers;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

/// <summary>
/// Manages <see cref="CompletionProvider"/>s and allows to get <see cref="CompletionItem"/>s correctly based on context.
/// </summary>
public sealed class CompletionService
{
    /// <summary>
    /// Creates a new <see cref="CompletionService"/> with the default <see cref="CompletionProvider"/>s.
    /// </summary>
    /// <param name="filter">The completion filter to use.</param>
    /// <returns>A new <see cref="CompletionService"/> with default providers.</returns>
    public static CompletionService CreateDefault(ICompletionFilter? filter)
    {
        var service = new CompletionService(filter ?? CompletionFilter.ContainsFilter);
        service.AddProvider(new KeywordCompletionProvider());
        service.AddProvider(new ExpressionCompletionProvider());
        service.AddProvider(new MemberCompletionProvider());
        return service;
    }

    private readonly ICompletionFilter completionFilter;
    private readonly List<CompletionProvider> providers = [];

    public CompletionService(ICompletionFilter filter)
    {
        this.completionFilter = filter;
    }

    /// <summary>
    /// Adds <see cref="CompletionProvider"/> this service can use.
    /// </summary>
    /// <param name="provider">The provider to add to this service.</param>
    public void AddProvider(CompletionProvider provider) => this.providers.Add(provider);

    /// <summary>
    /// Gets <see cref="CompletionItem"/>s from all applicable <see cref="CompletionProvider"/>s.
    /// </summary>
    /// <param name="semanticModel">The <see cref="SemanticModel"/> for the tree.</param>
    /// <param name="cursorIndex">Index of the cursor in the tree.</param>
    /// <returns><see cref="CompletionItem"/>s from all <see cref="CompletionProvider"/>s.</returns>
    public ImmutableArray<CompletionItem> GetCompletions(SemanticModel semanticModel, int cursorIndex)
    {
        var tree = semanticModel.Tree;

        var result = ImmutableArray.CreateBuilder<CompletionItem>();
        var currentContext = this.GetCurrentContexts(tree, cursorIndex);

        // Look for a filter node
        var cursorPosition = tree.IndexToSyntaxPosition(cursorIndex);
        var deepestNodeAtCursor = tree.TraverseSubtreesAtCursorPosition(cursorPosition).LastOrDefault();

        foreach (var provider in this.providers)
        {
            if (!provider.IsApplicableIn(currentContext)) continue;

            var completionItems = provider.GetCompletionItems(semanticModel, cursorIndex, currentContext);
            result.AddRange(completionItems.Where(i => this.completionFilter.ShouldKeep(deepestNodeAtCursor, i)));
        }
        return result.ToImmutable();
    }

    /// <summary>
    /// Gets current context based on location of <paramref name="cursorIndex"/> in the <paramref name="syntaxTree"/>.
    /// </summary>
    /// <param name="syntaxTree">The <see cref="SyntaxTree"/> in which to find contexts.</param>
    /// <param name="cursorIndex">The location in the <paramref name="syntaxTree"/>.</param>
    /// <returns>Flag enum of the currently valid <see cref="CompletionContext"/>s.</returns>
    private CompletionContext GetCurrentContexts(SyntaxTree syntaxTree, int cursorIndex)
    {
        var cursor = syntaxTree.IndexToSyntaxPosition(cursorIndex);
        var node = syntaxTree.Root.TraverseSubtreesAtCursorPosition(cursor).Last();
        return node switch
        {
            InlineFunctionBodySyntax => CompletionContext.Expression,
            BlockFunctionBodySyntax => CompletionContext.Declaration | CompletionContext.Expression,
            _ => node.Parent switch
            {
                // Special case, we are in a script
                NameExpressionSyntax { Parent: ScriptEntrySyntax } =>
                    CompletionContext.Declaration | CompletionContext.Expression,
                // Type expression
                NameTypeSyntax => CompletionContext.Type,
                // Parameter name declaration
                ParameterSyntax => CompletionContext.None,
                // Global declaration
                UnexpectedDeclarationSyntax => CompletionContext.Declaration,
                // Declaring identifier
                DeclarationSyntax => CompletionContext.None,
                // Member access
                MemberExpressionSyntax => CompletionContext.Expression | CompletionContext.Member,
                // Member type access
                MemberTypeSyntax => CompletionContext.Type | CompletionContext.Member,
                // Import member
                MemberImportPathSyntax => CompletionContext.Import | CompletionContext.Member,
                // Import start
                RootImportPathSyntax => CompletionContext.Import,
                // Global scope
                null => CompletionContext.Declaration,
                // Start of statement inside function
                _ when node.Parent?.Parent is ExpressionStatementSyntax =>
                    CompletionContext.Expression | CompletionContext.Declaration,
                _ => CompletionContext.Expression,
            },
        };
    }
}
