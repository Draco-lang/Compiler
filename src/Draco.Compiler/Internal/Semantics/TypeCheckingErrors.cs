using Draco.Compiler.Api.Diagnostics;

namespace Draco.Compiler.Internal.Semantics;

/// <summary>
/// Holds constants for type checking errors.
/// </summary>
internal static class TypeCheckingErrors
{
    private static string Code(int index) => DiagnosticTemplate.CreateDiagnosticCode(DiagnosticCategories.TypeChecking, index);

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
}

