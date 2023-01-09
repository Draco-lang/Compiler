using Draco.Compiler.Api.Diagnostics;

namespace Draco.Compiler.Internal.Semantics;

/// <summary>
/// Holds constants for semantic.
/// </summary>
internal static class SemanticErrors
{
    /// <summary>
    /// An undefined reference.
    /// </summary>
    public static readonly DiagnosticTemplate UndefinedReference = DiagnosticTemplate.Create(
        title: "undefined reference",
        severity: DiagnosticSeverity.Error,
        format: "undefined reference to {0}");

    /// <summary>
    /// The type of something could not be inferred.
    /// </summary>
    public static readonly DiagnosticTemplate CouldNotInferType = DiagnosticTemplate.Create(
        title: "could not infer type",
        severity: DiagnosticSeverity.Error,
        format: "could not infer type of {0}");

    /// <summary>
    /// A type mismatch error.
    /// </summary>
    public static readonly DiagnosticTemplate TypeMismatch = DiagnosticTemplate.Create(
        title: "type mismatch",
        severity: DiagnosticSeverity.Error,
        format: "type mismatch between {0} and {1}");

    /// <summary>
    /// A shadowing error.
    /// </summary>
    public static readonly DiagnosticTemplate IllegalShadowing = DiagnosticTemplate.Create(
        title: "illegal shadowing",
        severity: DiagnosticSeverity.Error,
        format: "symbol {0} illegally shadows symbol with the same name");

    /// <summary>
    /// A function does not return on all paths.
    /// </summary>
    public static readonly DiagnosticTemplate DoesNotReturn = DiagnosticTemplate.Create(
        title: "does not return",
        severity: DiagnosticSeverity.Error,
        format: "the function {0} does not return on all paths");

    /// <summary>
    /// A variable is used before it's initialized.
    /// </summary>
    public static readonly DiagnosticTemplate VariableUsedBeforeInit = DiagnosticTemplate.Create(
        title: "use of uninitialized variable",
        severity: DiagnosticSeverity.Error,
        format: "the variable {0} is used before initialized");
}
