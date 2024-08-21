using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeFixes;

/// <summary>
/// Allows to get <see cref="CodeFix"/>es from multiple <see cref="CodeFixProvider"/>s.
/// </summary>
public sealed class CodeFixService
{
    private readonly List<CodeFixProvider> providers = [];

    /// <summary>
    /// Adds <see cref="CodeFixProvider"/> this service can use.
    /// </summary>
    /// <param name="provider">The provider to add to this service.</param>
    public void AddProvider(CodeFixProvider provider) => this.providers.Add(provider);

    /// <summary>
    /// Gets <see cref="CodeFix"/>es from all registered <see cref="CodeFixProvider"/>s.
    /// </summary>
    /// <param name="tree">The <see cref="SyntaxTree"/> for which this service will create codefixes.</param>
    /// <param name="semanticModel">The <see cref="SemanticModel"/> for this <paramref name="tree"/>.</param>
    /// <param name="span">The <see cref="SourceSpan"/> to get the fixes for.</param>
    /// <returns><see cref="CodeFix"/>es from all registered <see cref="CodeFixProvider"/>s within <paramref name="span"/>.</returns>
    public ImmutableArray<CodeFix> GetCodeFixes(SyntaxTree tree, SemanticModel semanticModel, SourceSpan span)
    {
        var result = ImmutableArray.CreateBuilder<CodeFix>();
        foreach (var provider in this.providers)
        {
            foreach (var diagnostic in semanticModel.Diagnostics.IntersectBy(provider.DiagnosticCodes, x => x.Code))
            {
                result.AddRange(provider.GetCodeFixes(diagnostic, tree, span));
            }
        }
        return result.ToImmutable();
    }
}
