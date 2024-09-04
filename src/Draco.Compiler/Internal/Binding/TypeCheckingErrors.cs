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

    /// <summary>
    /// A generic function with the given no. args was not found.
    /// </summary>
    public static readonly DiagnosticTemplate NoGenericFunctionWithParamCount = DiagnosticTemplate.Create(
        title: "generic parameter count mismatch",
        severity: DiagnosticSeverity.Error,
        format: "function {0} with {1} number of generic parameters could not be found",
        code: Code(8));

    /// <summary>
    /// A generic type does not take the given amount of parameters.
    /// </summary>
    public static readonly DiagnosticTemplate GenericTypeParamCountMismatch = DiagnosticTemplate.Create(
        title: "generic parameter count mismatch",
        severity: DiagnosticSeverity.Error,
        format: "type {0} with {1} number of generic parameters could not be found",
        code: Code(9));

    /// <summary>
    /// The given expression can not be generic instantiated.
    /// </summary>
    public static readonly DiagnosticTemplate NotGenericConstruct = DiagnosticTemplate.Create(
        title: "not a generic construct",
        severity: DiagnosticSeverity.Error,
        format: "the given construct can not be generic instantiated",
        code: Code(10));

    /// <summary>
    /// The given variadic parameter type is illegal.
    /// </summary>
    public static readonly DiagnosticTemplate IllegalVariadicType = DiagnosticTemplate.Create(
        title: "illegal variadic type",
        severity: DiagnosticSeverity.Error,
        format: "the given type {0} is not legal for variadic parameters",
        code: Code(11));

    /// <summary>
    /// No common type found for types.
    /// Expected string of concatenated types as argument.
    /// </summary>
    public static readonly DiagnosticTemplate NoCommonType = DiagnosticTemplate.Create(
        title: "no common type found for types",
        severity: DiagnosticSeverity.Error,
        format: "no common type found for types {0}",
        code: Code(12));

    /// <summary>
    /// An expression was used in a context where a typed expression was expected.
    /// </summary>
    public static readonly DiagnosticTemplate IllegalExpression = DiagnosticTemplate.Create(
        title: "illegal expression",
        severity: DiagnosticSeverity.Error,
        format: "the expression is illegal in this context as it does not produce a value",
        code: Code(13));

    /// <summary>
    /// A non-attribute type was referenced in an attribute context.
    /// </summary>
    public static readonly DiagnosticTemplate NotAnAttribute = DiagnosticTemplate.Create(
        title: "non-attribute type used as attribute",
        severity: DiagnosticSeverity.Error,
        format: "the type {0} is not an attribute",
        code: Code(14));

    /// <summary>
    /// The attribute was applied to a not supported target element.
    /// </summary>
    public static readonly DiagnosticTemplate CanNotApplyAttribute = DiagnosticTemplate.Create(
        title: "can not apply attribute",
        severity: DiagnosticSeverity.Error,
        format: "the attribute {0} can not be applied to element type {1}",
        code: Code(15));
}

