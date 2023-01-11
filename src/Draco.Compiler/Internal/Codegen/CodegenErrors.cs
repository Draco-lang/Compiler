using System.Runtime.CompilerServices;
using Draco.Compiler.Api.Diagnostics;

namespace Draco.Compiler.Internal.Codegen;

internal static class CodegenErrors
{
    private static string ErrorCode(int index) => DiagnosticTemplate.CreateErrorCode(ErrorCategories.CodegenError, index);

    /// <summary>
    /// Scripting engine could not find main.
    /// </summary>
    public static readonly DiagnosticTemplate NoMainMethod = DiagnosticTemplate.Create(
        title: "no main method found",
        severity: DiagnosticSeverity.Error,
        format: "no main method found in compiled assembly",
        errorCode: ErrorCode(1));
}
