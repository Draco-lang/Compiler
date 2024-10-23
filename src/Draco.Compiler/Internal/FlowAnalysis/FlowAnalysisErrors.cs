using System.Diagnostics.CodeAnalysis;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.FlowAnalysis;

/// <summary>
/// Holds constants for flow analysis errors.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class FlowAnalysisErrors
{
    private static string Code(int index) => DiagnosticTemplate.CreateDiagnosticCode(DiagnosticCategory.FlowAnalysis, index);

    /// <summary>
    /// A function does not return on all paths.
    /// </summary>
    public static readonly DiagnosticTemplate DoesNotReturn = DiagnosticTemplate.Create(
        title: "does not return",
        severity: DiagnosticSeverity.Error,
        format: "the function {0} does not return on all paths",
        code: Code(1));

    /// <summary>
    /// A variable is used before it's initialized.
    /// </summary>
    public static readonly DiagnosticTemplate VariableUsedBeforeInit = DiagnosticTemplate.Create(
        title: "use of uninitialized variable",
        severity: DiagnosticSeverity.Error,
        format: "the variable {0} is used before initialized",
        code: Code(2));

    /// <summary>
    /// Immutable variable can not be assigned to.
    /// </summary>
    public static readonly DiagnosticTemplate ImmutableVariableAssignedMultipleTimes = DiagnosticTemplate.Create(
        title: "immutable variable assigned multiple times",
        severity: DiagnosticSeverity.Error,
        format: "the immutable variable {0} can only be assigned once",
        code: Code(3));
}
