using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Holds constants for symbol resolution errors.
/// </summary>
internal static class SymbolResolutionErrors
{
    // TODO: Look through where IllegalReference makes more sense than UndefinedReference

    private static string Code(int index) => DiagnosticTemplate.CreateDiagnosticCode(DiagnosticCategory.SymbolResolution, index);

    /// <summary>
    /// An undefined reference.
    /// </summary>
    public static readonly DiagnosticTemplate UndefinedReference = DiagnosticTemplate.Create(
        title: "undefined reference",
        severity: DiagnosticSeverity.Error,
        format: "undefined reference to {0}",
        code: Code(1));

    /// <summary>
    /// An illegal reference.
    /// </summary>
    public static readonly DiagnosticTemplate IllegalReference = DiagnosticTemplate.Create(
        title: "illegal reference",
        severity: DiagnosticSeverity.Error,
        format: "illegal reference to symbol {0}",
        code: Code(2));

    /// <summary>
    /// A shadowing error.
    /// </summary>
    public static readonly DiagnosticTemplate IllegalShadowing = DiagnosticTemplate.Create(
        title: "illegal shadowing",
        severity: DiagnosticSeverity.Error,
        format: "symbol {0} illegally shadows symbol with the same name",
        code: Code(3));

    /// <summary>
    /// Illegal lvalue.
    /// </summary>
    public static readonly DiagnosticTemplate IllegalLvalue = DiagnosticTemplate.Create(
        title: "illegal lvalue",
        severity: DiagnosticSeverity.Error,
        format: "illegal expression on the left-hand side of assignment",
        code: Code(4));
}

