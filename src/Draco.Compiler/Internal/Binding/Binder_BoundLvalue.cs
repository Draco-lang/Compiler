using System;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.UntypedTree;

namespace Draco.Compiler.Internal.Binding;

internal partial class Binder
{
    /// <summary>
    /// Binds the given untyped lvalue to a bound lvalue.
    /// </summary>
    /// <param name="lvalue">The untyped lvalue to bind.</param>
    /// <param name="constraints">The constraints that has been collected during the binding process.</param>
    /// <param name="diagnostics">The diagnostics produced during the process.</param>
    /// <returns>The bound lvalue for <paramref name="lvalue"/>.</returns>
    internal virtual BoundLvalue TypeLvalue(UntypedLvalue lvalue, ConstraintSolver constraints, DiagnosticBag diagnostics) => lvalue switch
    {
        UntypedUnexpectedLvalue unexpected => new BoundUnexpectedLvalue(unexpected.Syntax),
        UntypedIllegalLvalue illegal => new BoundIllegalLvalue(illegal.Syntax),
        UntypedLocalLvalue local => this.TypeLocalLvalue(local, constraints, diagnostics),
        UntypedGlobalLvalue global => this.TypeGlobalLvalue(global, constraints, diagnostics),
        UntypedFieldLvalue field => this.TypeFieldLvalue(field, constraints, diagnostics),
        UntypedMemberLvalue member => this.TypeMemberLvalue(member, constraints, diagnostics),
        _ => throw new ArgumentOutOfRangeException(nameof(lvalue)),
    };

    private BoundLvalue TypeLocalLvalue(UntypedLocalLvalue local, ConstraintSolver constraints, DiagnosticBag diagnostics) =>
        new BoundLocalLvalue(local.Syntax, constraints.GetTypedLocal(local.Local, diagnostics));

    private BoundLvalue TypeGlobalLvalue(UntypedGlobalLvalue global, ConstraintSolver constraints, DiagnosticBag diagnostics) =>
        new BoundGlobalLvalue(global.Syntax, global.Global);

    private BoundLvalue TypeFieldLvalue(UntypedFieldLvalue field, ConstraintSolver constraints, DiagnosticBag diagnostics) =>
        new BoundFieldLvalue(field.Syntax, field.Reciever is null ? null : this.TypeExpression(field.Reciever, constraints, diagnostics), field.Field);

    private BoundLvalue TypeMemberLvalue(UntypedMemberLvalue member, ConstraintSolver constraints, DiagnosticBag diagnostics) =>
        this.TypeMemberExpression(member.Expression, constraints, diagnostics) switch
        {
            BoundFieldExpression field => new BoundFieldLvalue(field.Syntax, field.Receiver, field.Field),
            _ => throw new InvalidOperationException(),
        };
}
