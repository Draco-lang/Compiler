using System.Collections.Immutable;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

public record class CompletionItem(TextChange Change, ImmutableArray<ISymbol> Symbols, CompletionKind Kind, params CompletionContext[] Context)
{
    public static CompletionItem Create(string text, CompletionKind kind, params CompletionContext[] contexts) =>
        new CompletionItem(new TextChange(null, null, text), ImmutableArray<ISymbol>.Empty, kind, contexts);

    public static CompletionItem Create(string text, ISymbol symbol, CompletionKind kind, params CompletionContext[] contexts) =>
        new CompletionItem(new TextChange(null, null, text), ImmutableArray.Create(symbol), kind, contexts);

    public static CompletionItem Create(string text, ImmutableArray<ISymbol> symbols, CompletionKind kind, params CompletionContext[] contexts) =>
        new CompletionItem(new TextChange(null, null, text), symbols, kind, contexts);
}

public record class TextChange(SyntaxPosition? Start, int? RemoveLength, string InsertedText);
