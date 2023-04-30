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
    /// The inference was incomplete.
    /// </summary>
    public static readonly DiagnosticTemplate InferenceIncomplete = DiagnosticTemplate.Create(
        title: "inference incomplete",
        severity: DiagnosticSeverity.Error,
        format: "type inference could not complete in {0}",
        code: Code(1));

    /// <summary>
    /// The type of something could not be inferred.
    /// </summary>
    public static readonly DiagnosticTemplate CouldNotInferType = DiagnosticTemplate.Create(
        title: "could not infer type",
        severity: DiagnosticSeverity.Error,
        format: "could not infer type of {0}",
        code: Code(2));

    /// <summary>
    /// A type mismatch error.
    /// </summary>
    public static readonly DiagnosticTemplate TypeMismatch = DiagnosticTemplate.Create(
        title: "type mismatch",
        severity: DiagnosticSeverity.Error,
        format: "type mismatch between {0} and {1}",
        code: Code(3));

    /// <summary>
    /// No matching overload found.
    /// </summary>
    public static readonly DiagnosticTemplate NoMatchingOverload = DiagnosticTemplate.Create(
        title: "no matching overload",
        severity: DiagnosticSeverity.Error,
        format: "no matching overload found for {0}",
        code: Code(4));

    /// <summary>
    /// More than one overload matches the call.
    /// </summary>
    public static readonly DiagnosticTemplate AmbiguousOverloadedCall = DiagnosticTemplate.Create(
        title: "ambiguous overload",
        severity: DiagnosticSeverity.Error,
        format: "ambiguous overloads found for {0}, candidates are {1}",
        code: Code(5));

    /// <summary>
    /// A function with matching parameters has already been defined.
    /// </summary>
    public static readonly DiagnosticTemplate IllegalOverloadDefinition = DiagnosticTemplate.Create(
        title: "illegal declaration",
        severity: DiagnosticSeverity.Error,
        format: "parameters of function {0} match another definition",
        code: Code(6));

    /// <summary>
    /// A non-function type was called.
    /// </summary>
    public static readonly DiagnosticTemplate CallNonFunction = DiagnosticTemplate.Create(
        title: "illegal call",
        severity: DiagnosticSeverity.Error,
        format: "the non-function type {0} can not be called",
        code: Code(7));
}

