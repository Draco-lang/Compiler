using System.Collections.Immutable;
using Draco.Compiler.Api.Diagnostics;

namespace Draco.Compiler.Api.CodeFixes;

public abstract class CodeFixProvider
{
    /// <summary>
    /// All codefixes the <see cref="CodeFixProvider"/> provides.
    /// </summary>
    internal abstract ImmutableArray<CodeFix> GetCodeFixes(ImmutableArray<Diagnostic> diagnostics);
}
