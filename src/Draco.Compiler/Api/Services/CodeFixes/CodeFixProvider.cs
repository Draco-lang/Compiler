using System.Collections.Immutable;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.Services.CodeFixes;

/// <summary>
/// Base class for providing <see cref="CodeFix"/>es.
/// </summary>
public abstract class CodeFixProvider
{
    /// <summary>
    /// The <see cref="Diagnostic"/> codes this <see cref="CodeFixProvider"/> can fix.
    /// </summary>
    public abstract ImmutableArray<string> DiagnosticCodes { get; }

    /// <summary>
    /// Gets all <see cref="CodeFix"/>es from this <see cref="CodeFixProvider"/>.
    /// </summary>
    /// <param name="diagnostic">Diagnostic for which the <see cref="CodeFixProvider"/> shold provide <see cref="CodeFix"/>es.</param>
    /// <param name="semanticModel">The <see cref="SemanticModel"/> for which the <see cref="CodeFix"/>es should be generated.</param>
    /// <param name="span">The <see cref="SourceSpan"/> for which the <see cref="CodeFix"/>es should be generated.</param>
    /// <returns>All <see cref="CodeFix"/>es from this <see cref="CodeFixProvider"/>.</returns>
    public abstract ImmutableArray<CodeFix> GetCodeFixes(Diagnostic diagnostic, SemanticModel semanticModel, SourceSpan span);
}
