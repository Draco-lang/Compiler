using System.Linq;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Error;
using Draco.Compiler.Internal.Symbols.Synthetized;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Solver;

internal sealed partial class ConstraintSolver
{
    private SolveState Solve(DiagnosticBag diagnostics, Constraint constraint) => constraint switch
    {
        SameTypeConstraint c => this.Solve(diagnostics, c),
        OverloadConstraint c => this.Solve(diagnostics, c),
        MemberConstraint c => this.Solve(diagnostics, c),
        CommonBaseConstraint c => this.Solve(diagnostics, c),
        BaseTypeConstraint c => this.Solve(diagnostics, c),
        _ => throw new System.ArgumentOutOfRangeException(nameof(constraint)),
    };

    private SolveState Solve(DiagnosticBag diagnostics, SameTypeConstraint constraint)
    {
        if (!this.Unify(constraint.First, constraint.Second))
        {
            var diagnostic = constraint.Diagnostic
                .WithTemplate(TypeCheckingErrors.TypeMismatch)
                .WithFormatArgs(this.Unwrap(constraint.First), this.Unwrap(constraint.Second))
                .Build();
            diagnostics.Add(diagnostic);
        }
        return SolveState.Finished;
    }

    private SolveState Solve(DiagnosticBag diagnostics, OverloadConstraint constraint)
    {
        var advanced = false;
        for (var i = 0; i < constraint.Candidates.Count;)
        {
            var candidate = constraint.Candidates[i];
            if (!this.Matches(candidate.Type, constraint.CallSite))
            {
                constraint.Candidates.RemoveAt(i);
                advanced = true;
            }
            else
            {
                ++i;
            }
        }
        // No overload matches
        if (constraint.Candidates.Count == 0)
        {
            // Best-effort shape approximation
            var errorSymbol = this.Unwrap(constraint.CallSite) is FunctionTypeSymbol functionType
                ? new NoOverloadFunctionSymbol(functionType.Parameters.Length)
                : new NoOverloadFunctionSymbol(1);
            this.Unify(errorSymbol.Type, constraint.CallSite);
            // Diagnostic, promise
            constraint.Promise.ConfigureDiagnostic(diag => diag
                .WithTemplate(TypeCheckingErrors.NoMatchingOverload)
                .WithFormatArgs(constraint.FunctionName));
            constraint.Promise.Fail(errorSymbol, diagnostics);
            return SolveState.Finished;
        }
        // Ok solve
        if (constraint.Candidates.Count == 1)
        {
            this.Unify(constraint.Candidates[0].Type, constraint.CallSite);
            constraint.Promise.Resolve(constraint.Candidates[0]);
            return SolveState.Finished;
        }
        // Depends if we removed anything
        return advanced ? SolveState.Progressing : SolveState.Stale;
    }

    private SolveState Solve(DiagnosticBag diagnostics, CommonBaseConstraint constraint)
    {
        if (this.Unwrap(constraint.First) is TypeVariable or NeverTypeSymbol && this.Unwrap(constraint.Second) is TypeVariable or NeverTypeSymbol)
        {
            foreach (var con in this.constraints)
            {
                if (con is BaseTypeConstraint basetype &&
                    (ReferenceEquals(basetype.Variable, constraint.First)
                    || ReferenceEquals(basetype.Variable, constraint.Second))) return SolveState.Stale;
            }
        }
        if (!this.UnifyBase(constraint.First, constraint.Second))
        {
            var diagnostic = constraint.Diagnostic
                .WithTemplate(TypeCheckingErrors.TypeMismatch)
                .WithFormatArgs(this.Unwrap(constraint.First), this.Unwrap(constraint.Second))
                .Build();
            diagnostics.Add(diagnostic);
        }
        return SolveState.Finished;
    }

    private SolveState Solve(DiagnosticBag diagnostics, BaseTypeConstraint constraint)
    {
        TypeSymbol GetInferedType(PrimitiveTypeSymbol builtinType)
        {
            if (builtinType == IntrinsicSymbols.IntegralType) return IntrinsicSymbols.Int32;
            else if (builtinType == IntrinsicSymbols.FloatingPointType) return IntrinsicSymbols.Float64;
            else throw new System.InvalidOperationException();
        }
        if (constraint.BaseType is not PrimitiveTypeSymbol baseType || !baseType.IsBaseType) throw new System.InvalidOperationException();
        var type = this.Unwrap(constraint.Variable);

        if (type is TypeVariable variable)
        {
            // Check if the TypeVariable is in any other constraints that fulfill certain rules, if so this constraint can't run yet

            foreach (var con in this.constraints)
            {
                if (con is SameTypeConstraint same
                    && (this.Unwrap(same.First).ContainsTypeVariable(variable)
                    || this.Unwrap(same.Second).ContainsTypeVariable(variable))
                    || con is CommonBaseConstraint common
                    && ((this.Unwrap(common.First).ContainsTypeVariable(variable)
                    && this.Unwrap(common.Second) is not (TypeVariable or NeverTypeSymbol))
                    || (this.Unwrap(common.Second).ContainsTypeVariable(variable)
                    && this.Unwrap(common.First) is not (TypeVariable or NeverTypeSymbol)))
                    || con is OverloadConstraint overload
                    && this.Unwrap(overload.CallSite).ContainsTypeVariable(variable)) return SolveState.Stale;
            }
        }
        else
        {
            // If this TypeVariable was already substituted just return
            if (type is PrimitiveTypeSymbol builtin)
            {
                var hasBase = false;
                foreach (var builtinBase in builtin.Bases)
                {
                    if (builtinBase == constraint.BaseType) hasBase = true;
                }
                if (hasBase) return SolveState.Finished;
            }
            else if (type is NeverTypeSymbol || type is ErrorTypeSymbol) return SolveState.Finished;
            var diagnostic = constraint.Diagnostic
                .WithTemplate(TypeCheckingErrors.TypeMismatch)
                .WithFormatArgs(this.Unwrap(type), this.Unwrap(GetInferedType(baseType)))
                .Build();
            diagnostics.Add(diagnostic);
            return SolveState.Finished;
        }

        // If it wasn't substituted yet, substitute it for the default type for given BaseType
        this.Substitute(constraint.Variable, GetInferedType(baseType));
        return SolveState.Finished;
    }

    private SolveState Solve(DiagnosticBag diagnostics, MemberConstraint constraint)
    {
        var accessed = this.Unwrap(constraint.Accessed);
        // We can't look up the members of type variables
        if (accessed.IsTypeVariable) return SolveState.Stale;

        // Not a type variable, we can look into members
        var membersWithName = accessed.Members
            .Where(m => m.Name == constraint.MemberName)
            .ToList();

        if (membersWithName.Count == 0)
        {
            constraint.Promise.ConfigureDiagnostic(diag => diag
                .WithTemplate(SymbolResolutionErrors.MemberNotFound)
                .WithFormatArgs(constraint.MemberName, this.Unwrap(constraint.Accessed)));
            constraint.Promise.Fail(new UndefinedMemberSymbol(), diagnostics);
        }
        else if (membersWithName.Count == 1)
        {
            // One member, just resolve to that
            var result = membersWithName[0];
            this.Unify(constraint.MemberType, ((ITypedSymbol)result).Type);
            constraint.Promise.Resolve(result);
        }
        else
        {
            // Multiple, overloading
            var methodsWithName = membersWithName.Cast<FunctionSymbol>();
            // We carry on this same promise
            var promise = constraint.Promise.Cast<Symbol, FunctionSymbol>();
            var overload = new OverloadConstraint(methodsWithName, constraint.MemberType, promise);
            this.constraints.Add(overload);
        }
        return SolveState.Finished;
    }

    private void FailSilently(Constraint constraint)
    {
        switch (constraint)
        {
        case OverloadConstraint overload:
            this.FailSilently(overload);
            break;
        default:
            throw new System.ArgumentOutOfRangeException(nameof(constraint));
        }
    }

    private void FailSilently(OverloadConstraint constraint)
    {
        // Best-effort shape approximation
        var errorSymbol = this.Unwrap(constraint.CallSite) is FunctionTypeSymbol functionType
            ? new NoOverloadFunctionSymbol(functionType.Parameters.Length)
            : new NoOverloadFunctionSymbol(1);
        constraint.Promise.FailSilently(errorSymbol);
    }

    private bool Matches(TypeSymbol left, TypeSymbol right)
    {
        left = this.Unwrap(left);
        right = this.Unwrap(right);

        if (ReferenceEquals(left, right)) return true;

        switch (left, right)
        {
        // Never type is never reached, matches everything
        case (NeverTypeSymbol, _):
        case (_, NeverTypeSymbol):
        // Error type matches everything to avoid cascading type errors
        case (ErrorTypeSymbol, _):
        case (_, ErrorTypeSymbol):
        // Type variables could match anything
        case (TypeVariable, _):
        case (_, TypeVariable):
            return true;

        // NOTE: Primitives are filtered out already, along with metadata types

        case (FunctionTypeSymbol f1, FunctionTypeSymbol f2):
        {
            if (f1.Parameters.Length != f2.Parameters.Length) return false;
            for (var i = 0; i < f1.Parameters.Length; ++i)
            {
                if (!this.Matches(f1.Parameters[i].Type, f2.Parameters[i].Type)) return false;
            }
            return this.Matches(f1.ReturnType, f2.ReturnType);
        }

        default:
            return false;
        }
    }

    private bool Unify(TypeSymbol left, TypeSymbol right)
    {
        left = this.Unwrap(left);
        right = this.Unwrap(right);

        if (ReferenceEquals(left, right)) return true;

        switch (left, right)
        {
        // Type variable substitution takes priority
        // so it can unify with never type and error type to stop type errors from cascading
        case (TypeVariable v1, TypeVariable v2):
        {
            // Check for circularity
            if (ReferenceEquals(v1, v2)) return true;
            this.Substitute(v1, v2);
            return true;
        }
        case (TypeVariable v, TypeSymbol other):
        {
            this.Substitute(v, other);
            return true;
        }
        case (TypeSymbol other, TypeVariable v):
        {
            this.Substitute(v, other);
            return true;
        }

        // Never type is never reached, unifies with everything
        case (NeverTypeSymbol, _):
        case (_, NeverTypeSymbol):
        // Error type unifies with everything to avoid cascading type errors
        case (ErrorTypeSymbol, _):
        case (_, ErrorTypeSymbol):
            return true;

        // NOTE: Primitives are filtered out already, along with metadata types

        case (FunctionTypeSymbol f1, FunctionTypeSymbol f2):
        {
            if (f1.Parameters.Length != f2.Parameters.Length) return false;
            for (var i = 0; i < f1.Parameters.Length; ++i)
            {
                if (!this.Unify(f1.Parameters[i].Type, f2.Parameters[i].Type)) return false;
            }
            return this.Unify(f1.ReturnType, f2.ReturnType);
        }

        default:
            return false;
        }
    }

    private bool UnifyBase(TypeSymbol left, TypeSymbol right)
    {
        left = this.Unwrap(left);
        right = this.Unwrap(right);

        switch (left, right)
        {
        // Type variable substitution takes priority
        // so it can unify with never type and error type to stop type errors from cascading
        case (TypeVariable v1, TypeVariable v2):
        {
            // Check for circularity
            if (ReferenceEquals(v1, v2)) return true;
            this.Substitute(v1, v2);
            return true;
        }
        case (TypeVariable v, TypeSymbol other):
        {
            this.Substitute(v, other);
            return true;
        }
        case (TypeSymbol other, TypeVariable v):
        {
            this.Substitute(v, other);
            return true;
        }

        // Never type is never reached, unifies with everything
        case (NeverTypeSymbol, _):
        case (_, NeverTypeSymbol):
        // Error type unifies with everything to avoid cascading type errors
        case (ErrorTypeSymbol, _):
        case (_, ErrorTypeSymbol):
            return true;

        case (PrimitiveTypeSymbol t1, PrimitiveTypeSymbol t2):
        {
            if (t1.IsBaseType)
            {
                var isBase = false;
                foreach (var baseType in t2.Bases)
                {
                    if (baseType.Name == t1.Name) isBase = true;
                }
                return isBase;
            }
            else if (t2.IsBaseType)
            {
                var isBase = false;
                foreach (var baseType in t1.Bases)
                {
                    if (baseType.Name == t2.Name) isBase = true;
                }
                return isBase;
            }
            else return ReferenceEquals(left, right);
        }

        case (FunctionTypeSymbol f1, FunctionTypeSymbol f2):
        {
            if (f1.Parameters.Length != f2.Parameters.Length) return false;
            for (var i = 0; i < f1.Parameters.Length; ++i)
            {
                if (!this.UnifyBase(f1.Parameters[i].Type, f2.Parameters[i].Type)) return false;
            }
            return this.UnifyBase(f1.ReturnType, f2.ReturnType);
        }

        default:
            throw new System.NotImplementedException();
        }
    }
}
