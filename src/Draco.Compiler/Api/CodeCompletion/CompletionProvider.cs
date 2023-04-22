using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

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
    /// <param name="tree">The <see cref="SyntaxTree"/> for which this service will create suggestions.</param>
    /// <param name="semanticModel">The <see cref="SemanticModel"/> for this <paramref name="tree"/>.</param>
    /// <param name="cursor">Position of cursor in the <paramref name="tree"/>.</param>
    /// <param name="contexts">Flag enum of current contexts.</param>
    /// <returns>All the <see cref="CompletionItem"/>s this <see cref="CompletionProvider"/> created.</returns>
    public abstract ImmutableArray<CompletionItem> GetCompletionItems(SyntaxTree tree, SemanticModel semanticModel, SyntaxPosition cursor, CompletionContext contexts);
}
