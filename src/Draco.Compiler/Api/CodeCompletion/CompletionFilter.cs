using System;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

/// <summary>
/// Utility for construction completion filters.
/// </summary>
public static class CompletionFilter
{
    /// <summary>
    /// Constructs a completion filter from a delegate.
    /// </summary>
    /// <param name="filter">The filter function.</param>
    /// <returns>The created completion filter.</returns>
    public static ICompletionFilter Create(Func<SyntaxToken?, CompletionItem, bool> filter) =>
        new DelegateCompletionFilter(filter);

    /// <summary>
    /// Constructs a completion filter that accepts all completion items when the token under the cursor is null.
    /// Otherwise, the provided filter is used.
    /// </summary>
    /// <param name="filter">The filter to use when the token is not null.</param>
    /// <returns>The created completion filter.</returns>
    public static ICompletionFilter AcceptNullToken(Func<SyntaxToken, CompletionItem, bool> filter) =>
        Create((token, item) => token is null || filter(token, item));

    private sealed class DelegateCompletionFilter(Func<SyntaxToken?, CompletionItem, bool> filter) : ICompletionFilter
    {
        public bool ShouldKeep(SyntaxToken? underCursor, CompletionItem item) => filter(underCursor, item);
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
    /// <param name="underCursor">The token under the cursor.</param>
    /// <param name="item">The completion item to check.</param>
    /// <returns>True, if the completion item should be kept.</returns>
    public bool ShouldKeep(SyntaxToken? underCursor, CompletionItem item);
}

