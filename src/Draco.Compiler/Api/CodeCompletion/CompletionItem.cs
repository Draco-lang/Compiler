using System.Collections.Immutable;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

/// <summary>
/// A single completion suggestion.
/// </summary>
public sealed class CompletionItem
{
    /// <summary>
    /// Creates a simple completion item that just replaces a single word.
    /// </summary>
    /// <param name="toReplace">The span of the text that will be replaced by <paramref name="toAdd"/>.</param>
    /// <param name="toAdd">The text that should be inserted into the source text.</param>
    /// <param name="kind">The kind of completion.</param>
    /// <param name="displayText">The text to display in the completion list.</param>
    /// <param name="filterText">The text to filter by in the completion list.</param>
    /// <param name="sortText">The text to sort by in the completion list.</param>
    /// <returns>The created completion item.</returns>
    public static CompletionItem Simple(
        SourceSpan toReplace,
        string toAdd,
        CompletionKind kind,
        string? displayText = null,
        string? filterText = null,
        string? sortText = null) =>
        Simple(new(toReplace, toAdd), kind, displayText, filterText, sortText);

    /// <summary>
    /// Creates a simple completion item that just replaces a single word.
    /// </summary>
    /// <param name="edit">The edit to apply to the document to insert this completion.</param>
    /// <param name="kind">The kind of completion.</param>
    /// <param name="displayText">The text to display in the completion list.</param>
    /// <param name="filterText">The text to filter by in the completion list.</param>
    /// <param name="sortText">The text to sort by in the completion list.</param>
    /// <returns>The created completion item.</returns>
    public static CompletionItem Simple(
        TextEdit edit,
        CompletionKind kind,
        string? displayText = null,
        string? filterText = null,
        string? sortText = null) =>
        new([edit], displayText ?? edit.Text, filterText ?? edit.Text, sortText ?? edit.Text, kind);

    /// <summary>
    /// The edit to apply to the document to insert this completion.
    /// </summary>
    public ImmutableArray<TextEdit> Edits { get; }

    /// <summary>
    /// The text to display in the completion list.
    /// </summary>
    public string DisplayText { get; }

    /// <summary>
    /// The text to filter by in the completion list.
    /// </summary>
    public string FilterText { get; }

    /// <summary>
    /// The text to sort by in the completion list.
    /// </summary>
    public string SortText { get; }

    /// <summary>
    /// The kind of completion.
    /// </summary>
    public CompletionKind Kind { get; }

    /// <summary>
    /// True, if this completion is simple, menaing it just replaces a single word.
    /// </summary>
    public bool IsSimple => this.Edits.Length == 1;

    private CompletionItem(
        ImmutableArray<TextEdit> edits,
        string displayText,
        string filterText,
        string sortText,
        CompletionKind kind)
    {
        this.Edits = edits;
        this.DisplayText = displayText;
        this.FilterText = filterText;
        this.SortText = sortText;
        this.Kind = kind;
    }
}
