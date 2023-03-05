using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.BoundTree;
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
    protected BoundStatement TypeStatement(UntypedStatement statement, ConstraintBag constraints, DiagnosticBag diagnostics) => statement switch
    {
        UntypedLocalDeclaration local => this.TypeLocalDeclaration(local, constraints, diagnostics),
        UntypedExpressionStatement expr => this.TypeExpressionStatement(expr, constraints, diagnostics),
        _ => throw new ArgumentOutOfRangeException(nameof(statement)),
    };

    private BoundStatement TypeLocalDeclaration(UntypedLocalDeclaration local, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        var typedValue = local.Value is null ? null : this.TypeExpression(local.Value, constraints, diagnostics);
        return new BoundLocalDeclaration(local.Syntax, local.Local, typedValue);
    }

    private BoundStatement TypeExpressionStatement(UntypedExpressionStatement expr, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        var typedExpr = this.TypeExpression(expr.Expression, constraints, diagnostics);
        return new BoundExpressionStatement(expr.Syntax, typedExpr);
    }
}
