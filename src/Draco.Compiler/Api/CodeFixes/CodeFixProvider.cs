using System.Collections.Immutable;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeFixes;

/// <summary>
/// Base class for providing <see cref="CodeFix"/>es.
/// </summary>
public abstract class CodeFixProvider
{
    public abstract ImmutableArray<string> DiagnosticCodes { get; }
    /// <summary>
    /// Gets all <see cref="CodeFix"/>es from this <see cref="CodeFixProvider"/>.
    /// </summary>
    /// <param name="diagnostic">Diagnostic for which the <see cref="CodeFixProvider"/> shold provide <see cref="CodeFix"/>es.</param>
    /// <param name="tree">The <see cref="SyntaxTree"/> for which the <see cref="CodeFix"/>es should be generated.</param>
    /// <param name="range">The <see cref="SyntaxRange"/> of the <see cref="Diagnostic"/>.</param>
    /// <returns>All <see cref="CodeFix"/>es from this <see cref="CodeFixProvider"/>.</returns>
    public abstract ImmutableArray<CodeFix> GetCodeFixes(Diagnostic diagnostic, SyntaxTree tree, SyntaxRange range);
}
