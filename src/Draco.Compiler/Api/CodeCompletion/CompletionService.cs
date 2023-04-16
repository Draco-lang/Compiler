using System.Collections.Generic;
using System.Collections.Immutable;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

public sealed class CompletionService
{
    private List<CompletionProvider> Providers = new List<CompletionProvider>();

    /// <summary>
    /// Adds <see cref="CompletionProvider"/> this service can use.
    /// </summary>
    /// <param name="provider">The provider to add to this service.</param>
    public void AddProvider(CompletionProvider provider) => this.Providers.Add(provider);

    /// <summary>
    /// Gets <see cref="CompletionItem"/>s from all registered <see cref="CompletionProvider"/>s.
    /// </summary>
    /// <param name="tree">The <see cref="SyntaxTree"/> for which this service will create suggestions.</param>
    /// <param name="semanticModel">The <see cref="SemanticModel"/> for this <paramref name="tree"/>.</param>
    /// <param name="cursor">Position of cursor in the <paramref name="tree"/>.</param>
    /// <returns><see cref="CompletionItem"/>s from all <see cref="CompletionProvider"/>s.</returns>
    public ImmutableArray<CompletionItem> GetCompletions(SyntaxTree tree, SemanticModel semanticModel, SyntaxPosition cursor)
    {
        var result = ImmutableArray.CreateBuilder<CompletionItem>();
        foreach (var provider in this.Providers)
        {
            result.AddRange(provider.GetCompletionItems(tree, semanticModel, cursor));
        }
        return result.ToImmutable();
    }
}
