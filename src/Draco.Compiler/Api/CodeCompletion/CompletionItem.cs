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
    public static CompletionItem Create(string text, SyntaxRange range, CompletionKind kind) =>
        new([new TextEdit(range, text)], text, [], kind);

    public static CompletionItem Create(string text, SyntaxRange range, ISymbol symbol, CompletionKind kind) =>
        new([new TextEdit(range, text)], text, [symbol], kind);

    public static CompletionItem Create(string text, SyntaxRange range, ImmutableArray<ISymbol> symbols, CompletionKind kind) =>
        new([new TextEdit(range, text)], text, symbols, kind);
}
