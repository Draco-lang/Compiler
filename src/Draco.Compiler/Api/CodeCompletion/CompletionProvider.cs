using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

/// <summary>
/// Base class for providing completions.
/// </summary>
public abstract class CompletionProvider
{
    /// <summary>
    /// Gets all <see cref="CompletionItem"/>s from this <see cref="CompletionProvider"/>.
    /// </summary>
    /// <param name="tree">The <see cref="SyntaxTree"/> for which this service will create suggestions.</param>
    /// <param name="semanticModel">The <see cref="SemanticModel"/> for this <paramref name="tree"/>.</param>
    /// <param name="cursor">Position of cursor in the <paramref name="tree"/>.</param>
    /// <returns>All the <see cref="CompletionItem"/>s this <see cref="CompletionProvider"/> created.</returns>
    internal abstract ImmutableArray<CompletionItem> GetCompletionItems(SyntaxTree tree, SemanticModel semanticModel, SyntaxPosition cursor, CompletionContext[] currentContexts);

    /// <summary>
    /// Array of completion contexts this <see cref="CompletionProvider"/> allows.
    /// </summary>
    internal abstract CompletionContext[] ValidContexts { get; }
}
