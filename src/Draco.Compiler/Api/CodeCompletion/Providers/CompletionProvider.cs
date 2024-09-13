using System.Collections.Immutable;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion.Providers;

/// <summary>
/// Provides completions for certain applicable contexts.
/// </summary>
public abstract class CompletionProvider
{
    /// <summary>
    /// Decides if this <see cref="CompletionProvider"/> can provide completions in the current <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The <see cref="CompletionContext"/> for which this <see cref="CompletionProvider"/> decides if it can provide completions.</param>
    /// <returns>True, if this <see cref="CompletionProvider"/> can provide completions in the current <paramref name="context"/>, otherwise false.</returns>
    public abstract bool IsApplicableIn(CompletionContext context);

    /// <summary>
    /// Gets all <see cref="CompletionItem"/>s from this <see cref="CompletionProvider"/>.
    /// </summary>
    /// <param name="semanticModel">The <see cref="SemanticModel"/> for the context.</param>
    /// <param name="cursorIndex">The index of the cursor in the tree.</param>
    /// <param name="nodeAtCursor">The <see cref="SyntaxNode"/> at the cursor position.</param>
    /// <param name="contexts">The current <see cref="CompletionContext"/>s.</param>
    /// <returns>The <see cref="CompletionItem"/>s this <see cref="CompletionProvider"/> proivded.</returns>
    public abstract ImmutableArray<CompletionItem> GetCompletionItems(
        SemanticModel semanticModel, int cursorIndex, SyntaxNode? nodeAtCursor, CompletionContext contexts);

    /// <summary>
    /// Translates a node to a <see cref="SourceSpan"/>, if it's a token. Otherwise constructs as 0-length span at the cursor.
    /// </summary>
    /// <param name="cursorIndex">The index of the cursor in the tree.</param>
    /// <param name="node">The <see cref="SyntaxNode"/> to translate to a <see cref="SourceSpan"/>.</param>
    /// <returns>The <see cref="SourceSpan"/> of the <paramref name="node"/> or a 0-length span at the cursor.</returns>
    protected static SourceSpan TokenSpan(int cursorIndex, SyntaxNode? node) =>
        (node as SyntaxToken)?.Span ?? new(cursorIndex, 0);
}
