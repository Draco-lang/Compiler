using System.Runtime.CompilerServices;
using Draco.Compiler.Api.Diagnostics;

namespace Draco.Compiler.Internal.Codegen;

internal static class CodegenErrors
{
    private static string Code(int index) => DiagnosticTemplate.CreateDiagnosticCode(DiagnosticCategories.Codegen, index);

    /// <summary>
    /// Scripting engine could not find main.
    /// </summary>
    public static readonly DiagnosticTemplate NoMainMethod = DiagnosticTemplate.Create(
        title: "no main method found",
        severity: DiagnosticSeverity.Error,
        format: "no main method found in compiled assembly",
        code: Code(1));
}
