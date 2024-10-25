using System.Collections.Immutable;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Api.Services.CodeCompletion.Providers;

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
    /// Checks, if a given <paramref name="symbol"/> is appropriate for the given <paramref name="context"/>.
    /// </summary>
    /// <param name="symbol">The <see cref="ISymbol"/> to check.</param>
    /// <param name="context">The <see cref="CompletionContext"/> to check against.</param>
    /// <returns>True, if the <paramref name="symbol"/> is appropriate for the given <paramref name="context"/>, otherwise false.</returns>
    protected static bool IsAppropriateForContext(ISymbol symbol, CompletionContext context) =>
        IsAppropriateForContext(((SymbolBase)symbol).Symbol, context);

    /// <summary>
    /// Checks, if a given <paramref name="symbol"/> is appropriate for the given <paramref name="context"/>.
    /// </summary>
    /// <param name="symbol">The <see cref="Symbol"/> to check.</param>
    /// <param name="context">The <see cref="CompletionContext"/> to check against.</param>
    /// <returns>True, if the <paramref name="symbol"/> is appropriate for the given <paramref name="context"/>, otherwise false.</returns>
    private protected static bool IsAppropriateForContext(Symbol symbol, CompletionContext context) => symbol.Kind switch
    {
        SymbolKind.Module => context.HasFlag(CompletionContext.Expression)
                          || context.HasFlag(CompletionContext.Type)
                          || context.HasFlag(CompletionContext.Import),

        SymbolKind.Label => context.HasFlag(CompletionContext.Declaration),

        SymbolKind.Type or SymbolKind.TypeParameter => context.HasFlag(CompletionContext.Expression)
                                                    || context.HasFlag(CompletionContext.Type),

        SymbolKind.Function
     or SymbolKind.FunctionGroup
     or SymbolKind.Local
     or SymbolKind.Parameter
     or SymbolKind.Field => context.HasFlag(CompletionContext.Expression),

        SymbolKind.Alias => IsAppropriateForContext(((Internal.Symbols.AliasSymbol)symbol).FullResolution, context),

        _ => true,
    };
}
