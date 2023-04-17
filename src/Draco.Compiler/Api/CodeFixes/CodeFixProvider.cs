using Draco.Compiler.Api.Diagnostics;

namespace Draco.Compiler.Api.CodeFixes;

public abstract class CodeFixProvider
{
    // TODO: terrible name
    internal abstract DiagnosticTemplate DiagnosticToFix { get; }


}
