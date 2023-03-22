using System;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.UntypedTree;

namespace Draco.Compiler.Internal.Binding;

internal partial class Binder
{
    /// <summary>
    /// Binds the given untyped statement to a bound statement.
    /// </summary>
    /// <param name="statement">The untyped statement to bind.</param>
    /// <param name="constraints">The constraints that has been collected during the binding process.</param>
    /// <param name="diagnostics">The diagnostics produced during the process.</param>
    /// <returns>The bound statement for <paramref name="statement"/>.</returns>
    internal virtual BoundStatement TypeStatement(UntypedStatement statement, ConstraintBag constraints, DiagnosticBag diagnostics) => statement switch
    {
        UntypedUnexpectedStatement unexpected => new BoundUnexpectedStatement(unexpected.Syntax),
        UntypedLabelStatement label => this.TypeLabelStatement(label, constraints, diagnostics),
        UntypedLocalDeclaration local => this.TypeLocalDeclaration(local, constraints, diagnostics),
        UntypedExpressionStatement expr => this.TypeExpressionStatement(expr, constraints, diagnostics),
        _ => throw new ArgumentOutOfRangeException(nameof(statement)),
    };

    private BoundStatement TypeLabelStatement(UntypedLabelStatement label, ConstraintBag constraints, DiagnosticBag diagnostics) =>
        new BoundLabelStatement(label.Syntax, label.Label);

    private BoundStatement TypeLocalDeclaration(UntypedLocalDeclaration local, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        var typedValue = local.Value is null ? null : this.TypeExpression(local.Value, constraints, diagnostics);
        var typedLocal = constraints.GetTypedLocal(diagnostics, local.Local);
        return new BoundLocalDeclaration(local.Syntax, typedLocal, typedValue);
    }

    private BoundStatement TypeExpressionStatement(UntypedExpressionStatement expr, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        var typedExpr = this.TypeExpression(expr.Expression, constraints, diagnostics);
        return new BoundExpressionStatement(expr.Syntax, typedExpr);
    }
}
