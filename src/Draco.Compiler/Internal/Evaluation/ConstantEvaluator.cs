using System;
using System.Text;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.OptimizingIr.Instructions;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Evaluation;

/// <summary>
/// Evaluates constant expressions.
/// </summary>
internal sealed class ConstantEvaluator(Compilation compilation)
{
    private WellKnownTypes WellKnownTypes => compilation.WellKnownTypes;

    /// <summary>
    /// Evaluates a bound expression.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="diagnostics">The diagnostics to report errors to.</param>
    /// <returns>The evaluated constant value.</returns>
    public ConstantValue Evaluate(BoundExpression expression, DiagnosticBag diagnostics) => expression switch
    {
        BoundLiteralExpression literal => new ConstantValue(literal.Type, literal.Value),
        BoundStringExpression str => this.Evaluate(str, diagnostics),
        _ => this.InvalidConstantExpression(expression, diagnostics),
    };

    private ConstantValue Evaluate(BoundStringExpression expression, DiagnosticBag diagnostics)
    {
        if (expression.Parts.Length == 1 && expression.Parts[0] is BoundStringText t)
        {
            return new ConstantValue(this.WellKnownTypes.SystemString, t.Text);
        }

        var result = new StringBuilder();
        foreach (var part in expression.Parts)
        {
            switch (part)
            {
            case BoundStringText text:
                result.Append(text.Text);
                break;
            case BoundStringInterpolation interpolation:
                var value = this.Evaluate(interpolation.Value, diagnostics);
                result.Append(value.Value);
                break;
            case BoundUnexpectedStringPart:
                break;
            default:
                throw new InvalidOperationException("unexpected string part");
            }
        }
        return new ConstantValue(this.WellKnownTypes.SystemString, result.ToString());
    }

    private ConstantValue InvalidConstantExpression(BoundExpression expression, DiagnosticBag diagnostics)
    {
        diagnostics.Add(Diagnostic.Create(
            template: EvaluationErrors.NotConstant,
            location: expression.Syntax?.Location));

        return new ConstantValue(WellKnownTypes.ErrorType, null);
    }
}
