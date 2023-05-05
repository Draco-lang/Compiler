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

    /// <summary>
    /// A member was not found in a type or module.
    /// </summary>
    public static readonly DiagnosticTemplate MemberNotFound = DiagnosticTemplate.Create(
        title: "member not found",
        severity: DiagnosticSeverity.Error,
        format: "member {0} could not be found in {1}",
        code: Code(5));

    /// <summary>
    /// A path points to an illegal import.
    /// </summary>
    public static readonly DiagnosticTemplate IllegalImport = DiagnosticTemplate.Create(
        title: "illegal import",
        severity: DiagnosticSeverity.Error,
        format: "the path {0} can not be imported",
        code: Code(6));

    /// <summary>
    /// A module was used as an expression.
    /// </summary>
    public static readonly DiagnosticTemplate IllegalModuleExpression = DiagnosticTemplate.Create(
        title: "illegal expression",
        severity: DiagnosticSeverity.Error,
        format: "the module name {0} is illegal in expression context",
        code: Code(7));

    /// <summary>
    /// A module was used as an expression.
    /// </summary>
    public static readonly DiagnosticTemplate IllegalModuleType = DiagnosticTemplate.Create(
        title: "illegal type",
        severity: DiagnosticSeverity.Error,
        format: "the module name {0} is illegal in type context",
        code: Code(8));

    /// <summary>
    /// Import is not at the top of the scope.
    /// </summary>
    public static readonly DiagnosticTemplate ImportNotAtTop = DiagnosticTemplate.Create(
        title: "import not at the top of the scope",
        severity: DiagnosticSeverity.Error,
        format: "import directives must appear at the top of the scope",
        code: Code(9));

    /// <summary>
    /// File path is outside of root path.
    /// </summary>
    public static readonly DiagnosticTemplate FilePathOutsideOfRootPath = DiagnosticTemplate.Create(
        title: "file path is outside of root path",
        severity: DiagnosticSeverity.Error,
        format: "the file path {0} is outside of the root path {1}",
        code: Code(10));
}
