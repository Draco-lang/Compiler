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
    /// <returns>A new <see cref="CompletionService"/> with default providers.</returns>
    public static CompletionService CreateDefault()
    {
        var service = new CompletionService();
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

        var result = ImmutableArray.CreateBuilder<CompletionItem>();
        var currentContext = this.GetCurrentContexts(tree, cursorIndex);

        // Look, if we are under an identifier token that can filter completions
        var cursorPosition = tree.IndexToSyntaxPosition(cursorIndex);
        var idAtCursor = tree.TraverseSubtreesAtCursorPosition(cursorPosition)
            .OfType<SyntaxToken>()
            .Where(x => x.Kind == TokenKind.Identifier || SyntaxFacts.IsKeyword(x.Kind))
            .LastOrDefault();

        foreach (var provider in this.providers)
        {
            if (!provider.IsApplicableIn(currentContext)) continue;

            var completionItems = provider.GetCompletionItems(semanticModel, cursorIndex, currentContext);
            if (idAtCursor is not null)
            {
                // Filter by the identifier at the cursor
                completionItems = this.FilterResultsByPrefixToken(idAtCursor, completionItems);
            }
            result.AddRange(completionItems);
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

    private ImmutableArray<CompletionItem> FilterResultsByPrefixToken(
        SyntaxToken atCursor,
        IEnumerable<CompletionItem> completionItems) => completionItems
        .Where(x => this.KeepCompletionItemByPrefixToken(atCursor, x))
        .ToImmutableArray();

    // TODO: This will be a nice place to put some fuzzy equality logic
    private bool KeepCompletionItemByPrefixToken(
        SyntaxToken atCursor,
        CompletionItem item)
    {
        // We don't deal with this
        if (item.Edits.Length != 1) return true;

        var replacementText = item.Edits[0].Text;
        return replacementText.Contains(atCursor.Text);
    }
}
