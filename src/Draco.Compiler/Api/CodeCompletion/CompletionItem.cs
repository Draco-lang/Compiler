using System.Collections.Immutable;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

/// <summary>
/// Represents a code completion item.
/// </summary>
/// <param name="Change">The <see cref="TextChange"/> this item provides.</param>
/// <param name="Symbols">All <see cref="ISymbol"/>s representing this completion (usually symbol representing type etc. or multiple <see cref="FunctionSymbol"/>s representing an overload).</param>
/// <param name="Kind">The <see cref="CompletionKind"/> of this completion.</param>
/// <param name="Context">The <see cref="CompletionContext"/> of this completion.</param>
public record class CompletionItem(TextChange Change, ImmutableArray<ISymbol> Symbols, CompletionKind Kind, params CompletionContext[] Context)
{
    public static CompletionItem Create(string text, CompletionKind kind, params CompletionContext[] contexts) =>
        new CompletionItem(new TextChange(null, null, text), ImmutableArray<ISymbol>.Empty, kind, contexts);

    public static CompletionItem Create(string text, ISymbol symbol, CompletionKind kind, params CompletionContext[] contexts) =>
        new CompletionItem(new TextChange(null, null, text), ImmutableArray.Create(symbol), kind, contexts);

    public static CompletionItem Create(string text, ImmutableArray<ISymbol> symbols, CompletionKind kind, params CompletionContext[] contexts) =>
        new CompletionItem(new TextChange(null, null, text), symbols, kind, contexts);
}

/// <summary>
/// Represents a change to source code.
/// </summary>
/// <param name="Start">The <see cref="SyntaxPosition"/> where this change starts.</param>
/// <param name="RemoveLength">The length of code that should be removed from the <paramref name="Start"/>.</param>
/// <param name="InsertedText">The text that should be inserted into the free space.</param>
public record class TextChange(SyntaxPosition? Start, int? RemoveLength, string InsertedText);
