using System.Collections.Immutable;
using Draco.Compiler.Api.Diagnostics;

namespace Draco.Compiler.Api.CodeFixes;

/// <summary>
/// Base class for providing <see cref="CodeFix"/>es.
/// </summary>
public abstract class CodeFixProvider
{
    /// <summary>
    /// Gets all <see cref="CodeFix"/>es from this <see cref="CodeFixProvider"/>.
    /// </summary>
    /// <param name="diagnostics">Current <see cref="Diagnostic"/>s.</param>
    /// <returns>All <see cref="CodeFix"/>es from this <see cref="CodeFixProvider"/>.</returns>
    public abstract ImmutableArray<CodeFix> GetCodeFixes(ImmutableArray<Diagnostic> diagnostics);
}
