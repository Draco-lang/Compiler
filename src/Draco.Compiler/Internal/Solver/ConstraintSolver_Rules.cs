using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols.Error;
using Draco.Compiler.Internal.Symbols;
using System.Diagnostics;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Solver;

internal sealed partial class ConstraintSolver
{
    private void HandleRule(SameTypeConstraint constraint, DiagnosticBag diagnostics)
    {
        for (var i = 1; i < constraint.Types.Length; ++i)
        {
            if (!this.Unify(constraint.Types[0], constraint.Types[i]))
            {
                // Type-mismatch
                constraint.Diagnostic
                    .WithTemplate(TypeCheckingErrors.TypeMismatch)
                    .WithFormatArgs(constraint.Types[0].Substitution, constraint.Types[i].Substitution);
                constraint.Promise.Fail(default, diagnostics);
                return;
            }
        }

        // Successful unification
        constraint.Promise.Resolve(default);
    }

    private void HandleRule(MemberConstraint constraint, DiagnosticBag diagnostics)
    {
        var accessed = constraint.Accessed.Substitution;
        // We can't advance on type variables
        if (accessed.IsTypeVariable)
        {
            throw new InvalidOperationException("rule handling for member constraint called prematurely");
        }

        // Not a type variable, we can look into members
        var membersWithName = accessed.InstanceMembers
            .Where(m => m.Name == constraint.MemberName)
            .ToImmutableArray();

        if (membersWithName.Length == 0)
        {
            // No such member, error
            constraint.Diagnostic
                .WithTemplate(SymbolResolutionErrors.MemberNotFound)
                .WithFormatArgs(constraint.MemberName, accessed);
            // We still provide a single error symbol
            var errorSymbol = new UndefinedMemberSymbol();
            this.Unify(constraint.MemberType, new ErrorTypeSymbol("<error>"));
            constraint.Promise.Fail(errorSymbol, diagnostics);
            return;
        }

        if (membersWithName.Length == 1)
        {
            // One member, we know what type the member type is
            this.Unify(((ITypedSymbol)membersWithName[0]).Type, constraint.MemberType);
            constraint.Promise.Resolve(membersWithName[0]);
            return;
        }

        // More than one, the member constraint is fine with multiple members but we don't know the member type
        {
            // All must be functions, otherwise we have bigger problems
            // TODO: Can this assertion fail? Like in a faulty module decl?
            Debug.Assert(membersWithName.All(m => m is FunctionSymbol));
            this.Unify(constraint.MemberType, new ErrorTypeSymbol("<error>"));
            var overload = new OverloadSymbol(membersWithName.Cast<FunctionSymbol>().ToImmutableArray());
            constraint.Promise.Resolve(overload);
        }
    }

    private void HandleRule<T>(AwaitConstraint<T> constraint)
    {
        // Wait until resolved
        if (!constraint.Awaited())
        {
            throw new InvalidOperationException("rule handling for await constraint called prematurely");
        }

        // We can resolve the awaited promise
        var mappedValue = constraint.Map();

        // Resolve this promise
        constraint.Promise.Resolve(mappedValue);
    }
}
