using System.Collections.Immutable;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

/// <summary>
/// Represents a suggestion for completing the code.
/// </summary>
/// <param name="Edits">The <see cref="TextEdit"/>s the completion would perform.</param>
/// <param name="Symbols">All <see cref="ISymbol"/>s representing this completion (usually symbol representing type etc. or multiple <see cref="FunctionSymbol"/>s representing an overload).</param>
/// <param name="Kind">The <see cref="CompletionKind"/> of this completion.</param>
public sealed record class CompletionItem(ImmutableArray<TextEdit> Edits, string DisplayText, ImmutableArray<ISymbol> Symbols, CompletionKind Kind)
{
    public static CompletionItem Create(SourceText source, string text, SourceSpan span, CompletionKind kind) =>
        new([new TextEdit(source, span, text)], text, [], kind);

    public static CompletionItem Create(SourceText source, string text, SourceSpan span, ISymbol symbol, CompletionKind kind) =>
        new([new TextEdit(source, span, text)], text, [symbol], kind);

    public static CompletionItem Create(SourceText source, string text, SourceSpan span, ImmutableArray<ISymbol> symbols, CompletionKind kind) =>
        new([new TextEdit(source, span, text)], text, symbols, kind);
}
