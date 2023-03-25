using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Holds constants for type checking errors.
/// </summary>
internal static class TypeCheckingErrors
{
    private static string Code(int index) => DiagnosticTemplate.CreateDiagnosticCode(DiagnosticCategory.TypeChecking, index);

    /// <summary>
    /// The type of something could not be inferred.
    /// </summary>
    public static readonly DiagnosticTemplate CouldNotInferType = DiagnosticTemplate.Create(
        title: "could not infer type",
        severity: DiagnosticSeverity.Error,
        format: "could not infer type of {0}",
        code: Code(1));

    /// <summary>
    /// A type mismatch error.
    /// </summary>
    public static readonly DiagnosticTemplate TypeMismatch = DiagnosticTemplate.Create(
        title: "type mismatch",
        severity: DiagnosticSeverity.Error,
        format: "type mismatch between {0} and {1}",
        code: Code(2));

    /// <summary>
    /// No matching overload found.
    /// </summary>
    public static readonly DiagnosticTemplate NoMatchingOverload = DiagnosticTemplate.Create(
        title: "no matching overload",
        severity: DiagnosticSeverity.Error,
        format: "no matching overload found for {0}",
        code: Code(3));

    /// <summary>
    /// The inference was incomplete.
    /// </summary>
    public static readonly DiagnosticTemplate InferenceIncomplete = DiagnosticTemplate.Create(
        title: "inference incomplete",
        severity: DiagnosticSeverity.Error,
        format: "type inference could not complete in {0}",
        code: Code(4));
}

