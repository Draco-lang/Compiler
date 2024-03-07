using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding.Tasks;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver.Utilities;

/// <summary>
/// Represents an argument for a call.
/// </summary>
/// <param name="Syntax">The syntax of the argument, if any.</param>
/// <param name="Type">The type of the argument.</param>
internal readonly record struct ArgumentDescription(SyntaxNode? Syntax, TypeSymbol Type)
{
    /// <summary>
    /// Constructs an argument for a call constraint.
    /// </summary>
    /// <param name="syntax">The argument syntax.</param>
    /// <param name="type">The argument type.</param>
    public ArgumentDescription Create(SyntaxNode? syntax, TypeSymbol type) => new(syntax, type);

    /// <summary>
    /// Constructs an argument for a call constraint.
    /// </summary>
    /// <param name="syntax">The argument syntax.</param>
    /// <param name="expression">The argument expression.</param>
    /// <param name="diagnostics">The diagnostics to report to.</param>
    /// <returns>The constructed argument descriptor.</returns>
    public ArgumentDescription Create(SyntaxNode? syntax, BindingTask<BoundExpression> expression, DiagnosticBag diagnostics) =>
        new(syntax, expression.GetResultType(syntax, this, diagnostics));

    /// <summary>
    /// Constructs an argument for a call constraint.
    /// </summary>
    /// <param name="syntax">The argument syntax.</param>
    /// <param name="lvalue">The argument lvalue.</param>
    /// <param name="diagnostics">The diagnostics to report to.</param>
    /// <returns>The constructed argument descriptor.</returns>
    public ArgumentDescription Create(SyntaxNode? syntax, BindingTask<BoundLvalue> lvalue, DiagnosticBag diagnostics) =>
        new(syntax, lvalue.GetResultType(syntax, this, diagnostics));
}
