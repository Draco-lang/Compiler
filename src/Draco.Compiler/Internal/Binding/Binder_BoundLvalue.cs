using System;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;
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

    private BoundLvalue TypeFieldLvalue(UntypedFieldLvalue field, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        if (!field.Field.IsMutable)
        {
            diagnostics.Add(Diagnostic.Create(
                template: SymbolResolutionErrors.CannotAssignToReadonlyOrConstantField,
                location: field.Syntax?.Location,
                field.Field.FullName));
            return new BoundIllegalLvalue(field.Syntax);
        }
        return new BoundFieldLvalue(field.Syntax, field.Reciever is null ? null : this.TypeExpression(field.Reciever, constraints, diagnostics), field.Field);
    }

    private BoundLvalue TypeMemberLvalue(UntypedMemberLvalue mem, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var left = this.TypeExpression(mem.Expression.Accessed, constraints, diagnostics);
        var members = mem.Expression.Member.Result;
        if (members.Length == 1 && members[0] is ITypedSymbol member)
        {
            if (member is FieldSymbol field)
            {
                if (!field.IsMutable)
                {
                    diagnostics.Add(Diagnostic.Create(
                        template: SymbolResolutionErrors.CannotAssignToReadonlyOrConstantField,
                        location: mem.Syntax?.Location,
                        field.FullName));
                    return new BoundIllegalLvalue(mem.Syntax);
                }
                return new BoundFieldLvalue(mem.Syntax, left, field);
            }
            diagnostics.Add(Diagnostic.Create(
                template: SymbolResolutionErrors.IllegalLvalue,
                location: mem.Syntax?.Location));
            return new BoundIllegalLvalue(mem.Syntax);
        }
        else
        {
            // NOTE: I'm not sure this can happen
            // Multiple members can maybe happen, in case there are duplicates, in which case this would be a cascaded
            // error
            // TODO: Verify
            throw new NotImplementedException();
        }
    }
}
