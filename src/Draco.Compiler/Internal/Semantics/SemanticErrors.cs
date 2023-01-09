using Draco.Compiler.Api.Diagnostics;

namespace Draco.Compiler.Internal.Semantics;

/// <summary>
/// Holds constants for semantic.
/// </summary>
internal static class SemanticErrors
{
    private static string SymbolResolutionErrorCode(int index) => DiagnosticTemplate.CreateErrorCode(ErrorCategories.SymbolResolutionError, index);
    private static string TypeCheckingErrorCode(int index) => DiagnosticTemplate.CreateErrorCode(ErrorCategories.TypeCheckingError, index);
    private static string DataflowErrorCode(int index) => DiagnosticTemplate.CreateErrorCode(ErrorCategories.DataflowError, index);

    /// <summary>
    /// An undefined reference.
    /// </summary>
    public static readonly DiagnosticTemplate UndefinedReference = DiagnosticTemplate.Create(
        title: "undefined reference",
        severity: DiagnosticSeverity.Error,
        format: "undefined reference to {0}",
        errorCode: SymbolResolutionErrorCode(1));

    /// <summary>
    /// The type of something could not be inferred.
    /// </summary>
    public static readonly DiagnosticTemplate CouldNotInferType = DiagnosticTemplate.Create(
        title: "could not infer type",
        severity: DiagnosticSeverity.Error,
        format: "could not infer type of {0}",
        errorCode: TypeCheckingErrorCode(1));

    /// <summary>
    /// A type mismatch error.
    /// </summary>
    public static readonly DiagnosticTemplate TypeMismatch = DiagnosticTemplate.Create(
        title: "type mismatch",
        severity: DiagnosticSeverity.Error,
        format: "type mismatch between {0} and {1}",
        errorCode: TypeCheckingErrorCode(2));

    /// <summary>
    /// A shadowing error.
    /// </summary>
    public static readonly DiagnosticTemplate IllegalShadowing = DiagnosticTemplate.Create(
        title: "illegal shadowing",
        severity: DiagnosticSeverity.Error,
        format: "symbol {0} illegally shadows symbol with the same name",
        errorCode: SymbolResolutionErrorCode(2));
}
