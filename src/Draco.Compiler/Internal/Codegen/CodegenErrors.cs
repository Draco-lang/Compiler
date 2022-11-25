using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Diagnostics;

namespace Draco.Compiler.Internal.Codegen;

internal static class CodegenErrors
{
    /// <summary>
    /// Backend compilation error.
    /// </summary>
    public static readonly DiagnosticTemplate Roslyn = DiagnosticTemplate.Create(
        title: "roslyn error",
        severity: DiagnosticSeverity.Error,
        format: "roslyn reported an error while compiling the generated C# code {0}");

    /// <summary>
    /// Scripting engine could not find main.
    /// </summary>
    public static readonly DiagnosticTemplate NoMainMethod = DiagnosticTemplate.Create(
        title: "no main method found",
        severity: DiagnosticSeverity.Error,
        format: "no main method found in compiled assembly");
}
