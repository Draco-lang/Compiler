using System;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

/// <summary>
/// Utility for construction completion filters.
/// </summary>
public static class CompletionFilter
{
    /// <summary>
    /// A filter that accepts all completion items that contain the text under the cursor.
    /// </summary>
    public static ICompletionFilter ContainsFilter { get; } = NameFilter((text, item) => item.FilterText.Contains(text));

    /// <summary>
    /// Constructs a completion filter from a delegate.
    /// </summary>
    /// <param name="filter">The filter function.</param>
    /// <returns>The created completion filter.</returns>
    public static ICompletionFilter Create(Func<SyntaxNode?, CompletionItem, bool> filter) =>
        new DelegateCompletionFilter(filter);

    /// <summary>
    /// Constructs a completion filter that accepts all completion items when the node under the cursor is null.
    /// Otherwise, the provided filter is used.
    /// </summary>
    /// <param name="filter">The filter to use when the token is not null.</param>
    /// <returns>The created completion filter.</returns>
    public static ICompletionFilter AcceptNull(Func<SyntaxNode, CompletionItem, bool> filter) =>
        Create((node, item) => node is null || filter(node, item));

    /// <summary>
    /// Constructs a completion filter that looks at text-like tokens and filters based on the provided function.
    /// </summary>
    /// <param name="filter">The filter function.</param>
    /// <returns>The created completion filter.</returns>
    public static ICompletionFilter NameFilter(Func<string, CompletionItem, bool> filter) => AcceptNull((node, item) =>
        (node is not SyntaxToken token || (token.Kind != TokenKind.Identifier && !SyntaxFacts.IsKeyword(token.Kind)))
        || filter(token.Text, item));

    private sealed class DelegateCompletionFilter(Func<SyntaxNode?, CompletionItem, bool> filter) : ICompletionFilter
    {
        public bool ShouldKeep(SyntaxNode? underCursor, CompletionItem item) => filter(underCursor, item);
    }
}

/// <summary>
/// A filter that can be used to filter completion items.
/// </summary>
public interface ICompletionFilter
{
    /// <summary>
    /// Determines if the completion item should be kept.
    /// </summary>
    /// <param name="underCursor">The syntax node under the cursor.</param>
    /// <param name="item">The completion item to check.</param>
    /// <returns>True, if the completion item should be kept.</returns>
    public bool ShouldKeep(SyntaxNode? underCursor, CompletionItem item);
}

