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
public sealed class CompletionService(ICompletionFilter filter)
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

    private readonly List<CompletionProvider> providers = [];

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

        // Look for a filter node
        var cursorPosition = tree.IndexToSyntaxPosition(cursorIndex);
        var deepestNodeAtCursor = tree.TraverseSubtreesAtCursorPosition(cursorPosition).LastOrDefault();

        // Get the current context
        var currentContext = GetCurrentContexts(deepestNodeAtCursor);

        var result = ImmutableArray.CreateBuilder<CompletionItem>();
        foreach (var provider in this.providers)
        {
            if (!provider.IsApplicableIn(currentContext)) continue;

            var completionItems = provider.GetCompletionItems(semanticModel, cursorIndex, deepestNodeAtCursor, currentContext);
            result.AddRange(completionItems.Where(i => filter.ShouldKeep(deepestNodeAtCursor, i)));
        }
        return result.ToImmutable();
    }

    /// <summary>
    /// Gets current context based on the node at the cursor.
    /// </summary>
    /// <param name="nodeAtCursor">The syntax node at the cursor.</param>
    /// <returns>Flag enum of the currently valid <see cref="CompletionContext"/>s.</returns>
    private static CompletionContext GetCurrentContexts(SyntaxNode? nodeAtCursor) => nodeAtCursor switch
    {
        null => CompletionContext.Declaration,
        InlineFunctionBodySyntax => CompletionContext.Expression,
        BlockFunctionBodySyntax => CompletionContext.Declaration | CompletionContext.Expression,
        _ => nodeAtCursor.Parent switch
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
            _ when nodeAtCursor.Parent?.Parent is ExpressionStatementSyntax =>
                CompletionContext.Expression | CompletionContext.Declaration,
            _ => CompletionContext.Expression,
        },
    };
}
