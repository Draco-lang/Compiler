using System;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
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
    internal virtual BoundStatement TypeStatement(UntypedStatement statement, ConstraintSolver constraints, DiagnosticBag diagnostics) => statement switch
    {
        UntypedUnexpectedStatement unexpected => new BoundUnexpectedStatement(unexpected.Syntax),
        UntypedSequencePointStatement sequence => this.TypeSequencePointStatement(sequence, constraints, diagnostics),
        UntypedNoOpStatement noOp => this.TypeNoOpStatement(noOp, constraints, diagnostics),
        UntypedLabelStatement label => this.TypeLabelStatement(label, constraints, diagnostics),
        UntypedLocalDeclaration local => this.TypeLocalDeclaration(local, constraints, diagnostics),
        UntypedExpressionStatement expr => this.TypeExpressionStatement(expr, constraints, diagnostics),
        _ => throw new ArgumentOutOfRangeException(nameof(statement)),
    };

    private BoundStatement TypeSequencePointStatement(UntypedSequencePointStatement sequence, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var stmt = sequence.Statement is null
            ? null
            : this.TypeStatement(sequence.Statement, constraints, diagnostics);
        return new BoundSequencePointStatement(sequence.Syntax, stmt, sequence.Range);
    }

    private BoundStatement TypeNoOpStatement(UntypedNoOpStatement noOp, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        if (noOp.Syntax is null) return BoundNoOpStatement.Default;
        return new BoundNoOpStatement(noOp.Syntax);
    }

    private BoundStatement TypeLabelStatement(UntypedLabelStatement label, ConstraintSolver constraints, DiagnosticBag diagnostics) =>
        new BoundLabelStatement(label.Syntax, label.Label);

    private BoundStatement TypeLocalDeclaration(UntypedLocalDeclaration local, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var typedValue = local.Value is null ? null : this.TypeExpression(local.Value, constraints, diagnostics);
        var typedLocal = constraints.GetTypedLocal(local.Local, diagnostics);
        return new BoundLocalDeclaration(local.Syntax, typedLocal, typedValue);
    }

    private BoundStatement TypeExpressionStatement(UntypedExpressionStatement expr, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var typedExpr = this.TypeExpression(expr.Expression, constraints, diagnostics);
        return new BoundExpressionStatement(expr.Syntax, typedExpr);
    }
}
