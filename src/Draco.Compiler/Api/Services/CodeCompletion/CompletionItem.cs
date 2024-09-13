using System;
using System.Collections.Immutable;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.Services.CodeCompletion;

/// <summary>
/// A single completion suggestion.
/// </summary>
public sealed class CompletionItem
{
    /// <summary>
    /// Creates a simple completion item that just replaces a single word for a symbol reference.
    /// </summary>
    /// <param name="toReplace">The span of the text that will be replaced by <paramref name="symbol"/>.</param>
    /// <param name="symbol">The symbol to insert into the source text.</param>
    /// <param name="fuallyQualify">True, if the symbol should be fully qualified.</param>
    /// <returns>The created completion item.</returns>
    public static CompletionItem Simple(SourceSpan toReplace, ISymbol symbol, bool fuallyQualify = false)
    {
        var replacementText = fuallyQualify ? symbol.FullName : symbol.Name;
        var detailsText = symbol switch
        {
            IFunctionGroupSymbol g => $"{g.Functions.Length} overloads",
            _ => null,
        };
        return new(
            edits: [new(toReplace, replacementText)],
            symbol: symbol,
            displayText: replacementText,
            filterText: replacementText,
            sortText: $"{symbol.Kind}_{symbol.Name}",
            detailsText: detailsText,
            kind: ToCompletionKind(symbol));
    }

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
        string? sortText = null) => new(
            edits: [edit],
            symbol: null,
            displayText: displayText ?? edit.Text,
            filterText: filterText ?? edit.Text,
            sortText: sortText ?? $"{kind}_{edit.Text}",
            detailsText: null,
            kind: kind);

    private static CompletionKind ToCompletionKind(ISymbol symbol) => symbol.Kind switch
    {
        SymbolKind.Module => CompletionKind.ModuleName,
        SymbolKind.Field => CompletionKind.FieldName,
        SymbolKind.Property => CompletionKind.PropertyName,
        SymbolKind.Global => CompletionKind.VariableName,
        SymbolKind.Local => CompletionKind.VariableName,
        SymbolKind.Parameter => CompletionKind.ParameterName,
        SymbolKind.Function => CompletionKind.FunctionName,
        SymbolKind.FunctionGroup => CompletionKind.FunctionName,
        SymbolKind.Type => ((ITypeSymbol)symbol).IsValueType ? CompletionKind.ValueTypeName : CompletionKind.ReferenceTypeName,
        SymbolKind.TypeParameter => CompletionKind.TypeParameterName,
        SymbolKind.Alias => ToCompletionKind(((IAliasSymbol)symbol).FullResolution),
        SymbolKind.Label => CompletionKind.LabelName,
        _ => throw new ArgumentOutOfRangeException(nameof(symbol)),
    };

    /// <summary>
    /// The edit to apply to the document to insert this completion.
    /// </summary>
    public ImmutableArray<TextEdit> Edits { get; }

    /// <summary>
    /// The symbol associated with this completion, if any.
    /// </summary>
    public ISymbol? Symbol { get; }

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
    /// The details text to display in the completion list, if any.
    /// </summary>
    public string? DetailsText { get; }

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
        ISymbol? symbol,
        string displayText,
        string filterText,
        string sortText,
        string? detailsText,
        CompletionKind kind)
    {
        this.Edits = edits;
        this.Symbol = symbol;
        this.DisplayText = displayText;
        this.FilterText = filterText;
        this.SortText = sortText;
        this.DetailsText = detailsText;
        this.Kind = kind;
    }
}
