using System.Diagnostics.CodeAnalysis;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.Codegen;

[ExcludeFromCodeCoverage]
internal static class CodegenErrors
{
    private static string Code(int index) => DiagnosticTemplate.CreateDiagnosticCode(DiagnosticCategory.Codegen, index);

    /// <summary>
    /// Scripting engine could not find main.
    /// </summary>
    public static readonly DiagnosticTemplate NoMainMethod = DiagnosticTemplate.Create(
        title: "no main method found",
        severity: DiagnosticSeverity.Error,
        format: "no main method found in compiled assembly",
        code: Code(1));
}
