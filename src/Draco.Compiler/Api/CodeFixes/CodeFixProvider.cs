using System.Collections.Immutable;
using Draco.Compiler.Api.Diagnostics;

namespace Draco.Compiler.Api.CodeFixes;

public abstract class CodeFixProvider
{
    // TODO: terrible name
    /// <summary>
    /// The <see cref="DiagnosticTemplate"/> this <see cref="CodeFixProvider"/> can fix.
    /// </summary>
    internal abstract DiagnosticTemplate DiagnosticToFix { get; }

    /// <summary>
    /// All codefixes the <see cref="CodeFixProvider"/> provides.
    /// </summary>
    internal abstract ImmutableArray<CodeFix> CodeFixes { get; }
}
