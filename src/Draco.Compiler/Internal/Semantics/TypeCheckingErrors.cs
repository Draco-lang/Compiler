using Draco.Compiler.Api.Diagnostics;

namespace Draco.Compiler.Internal.Semantics;

/// <summary>
/// Holds constants for type checking errors.
/// </summary>
internal static class TypeCheckingErrors
{
    private static string ErrorCode(int index) => DiagnosticTemplate.CreateErrorCode(ErrorCategories.TypeCheckingError, index);

    /// <summary>
    /// The type of something could not be inferred.
    /// </summary>
    public static readonly DiagnosticTemplate CouldNotInferType = DiagnosticTemplate.Create(
        title: "could not infer type",
        severity: DiagnosticSeverity.Error,
        format: "could not infer type of {0}",
        errorCode: ErrorCode(1));

    /// <summary>
    /// A type mismatch error.
    /// </summary>
    public static readonly DiagnosticTemplate TypeMismatch = DiagnosticTemplate.Create(
        title: "type mismatch",
        severity: DiagnosticSeverity.Error,
        format: "type mismatch between {0} and {1}",
        errorCode: ErrorCode(2));
}

