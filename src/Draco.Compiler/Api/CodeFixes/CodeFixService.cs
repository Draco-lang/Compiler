using Draco.Compiler.Api.CodeCompletion;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Draco.Compiler.Api.CodeFixes;

public sealed class CodeFixService
{
    private List<CodeFixProvider> Providers = new List<CodeFixProvider>();

    /// <summary>
    /// Adds <see cref="CodeFixProvider"/> this service can use.
    /// </summary>
    /// <param name="provider">The provider to add to this service.</param>
    public void AddProvider(CodeFixProvider provider) => this.Providers.Add(provider);

    /// <summary>
    /// Gets <see cref="CodeFix"/>es from all registered <see cref="CodeFixProvider"/>s.
    /// </summary>
    /// <param name="tree">The <see cref="SyntaxTree"/> for which this service will create codefixes.</param>
    /// <param name="semanticModel">The <see cref="SemanticModel"/> for this <paramref name="tree"/>.</param>
    /// <returns><see cref="CodeFix"/>es from all <see cref="CodeFixProvider"/>s.</returns>
    public ImmutableArray<CodeFix> GetCodeFixes(SyntaxTree tree, SemanticModel semanticModel)
    {
        var result = ImmutableArray.CreateBuilder<CodeFix>();
        var diags = semanticModel.Diagnostics.Select(x => x.Template);
        foreach (var provider in this.Providers)
        {
            if (diags.Contains(provider.DiagnosticToFix))
            {
                result.AddRange(provider.CodeFixes);
            }
        }
        return result.ToImmutable();
    }
}
