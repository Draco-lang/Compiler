using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.FlowAnalysis;

/// <summary>
/// Holds constants for flow analysis errors.
/// </summary>
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

    // TODO: Is this really a dataflow error?
    /// <summary>
    /// Illegal value on left side of assignment.
    /// </summary>
    public static readonly DiagnosticTemplate IllegalLValue = DiagnosticTemplate.Create(
        title: "illegal lvaule",
        severity: DiagnosticSeverity.Error,
        format: "illegal value on the left side of assignment",
        code: Code(3));

    /// <summary>
    /// Immutable variable must be initialized at declaration site.
    /// </summary>
    public static readonly DiagnosticTemplate ImmutableVariableMustBeInitialized = DiagnosticTemplate.Create(
        title: "immutable variable must be initialized",
        severity: DiagnosticSeverity.Error,
        format: "the immutable variable {0} must be initialized",
        code: Code(4));

    /// <summary>
    /// Immutable variable can not be assigned to.
    /// </summary>
    public static readonly DiagnosticTemplate ImmutableVariableCanNotBeAssignedTo = DiagnosticTemplate.Create(
        title: "immutable variable can not be assigned to",
        severity: DiagnosticSeverity.Error,
        format: "the immutable variable {0} can not be assigned to, it is read only",
        code: Code(5));
}
