using Draco.Compiler.Api.Diagnostics;

namespace Draco.Compiler.Internal.Codegen;

internal static class CodegenErrors
{
    /// <summary>
    /// Scripting engine could not find main.
    /// </summary>
    public static readonly DiagnosticTemplate NoMainMethod = DiagnosticTemplate.Create(
        title: "no main method found",
        severity: DiagnosticSeverity.Error,
        format: "no main method found in compiled assembly",
        errorCode: DiagnosticTemplate.SyntaxErrorCode(4, 1));
}
