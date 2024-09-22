using System.Diagnostics.CodeAnalysis;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.Evaluation;

/// <summary>
/// Error messages for the evaluation phase.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class EvaluationErrors
{
    private static string Code(int index) => DiagnosticTemplate.CreateDiagnosticCode(DiagnosticCategory.ConstantEvaluation, index);

    /// <summary>
    /// The expression is not a valid constant expression.
    /// </summary>
    public static readonly DiagnosticTemplate NotConstant = DiagnosticTemplate.Create(
        title: "not a constant",
        severity: DiagnosticSeverity.Error,
        format: "the expression is not a valid constant expression",
        code: Code(1));
}
