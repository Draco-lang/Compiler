using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

/// <summary>
/// Utility for construction completion filters.
/// </summary>
public static class CompletionFilter
{
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

