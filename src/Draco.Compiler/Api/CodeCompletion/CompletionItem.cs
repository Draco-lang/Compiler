using System.Collections.Immutable;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

/// <summary>
/// Represents a code completion item.
/// </summary>
/// <param name="Edit">The <see cref="TextEdit"/> this item provides.</param>
/// <param name="Symbols">All <see cref="ISymbol"/>s representing this completion (usually symbol representing type etc. or multiple <see cref="FunctionSymbol"/>s representing an overload).</param>
/// <param name="Kind">The <see cref="CompletionKind"/> of this completion.</param>
public sealed record class CompletionItem(TextEdit Edit, ImmutableArray<ISymbol> Symbols, CompletionKind Kind)
{
    public static CompletionItem Create(string text, CompletionKind kind) =>
        new CompletionItem(new TextEdit(text, SyntaxRange.Empty), ImmutableArray<ISymbol>.Empty, kind);

    public static CompletionItem Create(string text, ISymbol symbol, CompletionKind kind) =>
        new CompletionItem(new TextEdit(text, SyntaxRange.Empty), ImmutableArray.Create(symbol), kind);

    public static CompletionItem Create(string text, ImmutableArray<ISymbol> symbols, CompletionKind kind) =>
        new CompletionItem(new TextEdit(text, SyntaxRange.Empty), symbols, kind);
}
