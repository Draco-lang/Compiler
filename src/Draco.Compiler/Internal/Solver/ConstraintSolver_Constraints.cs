using System.Linq;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols.Error;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Solver;

internal sealed partial class ConstraintSolver
{
    private SolveState Solve(DiagnosticBag diagnostics, Constraint constraint) => constraint switch
    {
        SameTypeConstraint c => this.Solve(diagnostics, c),
        OverloadConstraint c => this.Solve(diagnostics, c),
        CommonBaseConstraint c => this.Solve(diagnostics, c),
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
            var errorSymbol = this.Unwrap(constraint.CallSite) is FunctionType functionType
                ? new NoOverloadFunctionSymbol(functionType.ParameterTypes.Length)
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

    private void FailSilently(Constraint constraint)
    {
        switch (constraint)
        {
        case OverloadConstraint overload:
            this.FailSilently(overload);
            break;
        }
    }

    private void FailSilently(OverloadConstraint constraint)
    {
        // Best-effort shape approximation
        var errorSymbol = this.Unwrap(constraint.CallSite) is FunctionType functionType
            ? new NoOverloadFunctionSymbol(functionType.ParameterTypes.Length)
            : new NoOverloadFunctionSymbol(1);
        constraint.Promise.FailSilently(errorSymbol);
    }

    private bool Matches(Type left, Type right)
    {
        left = this.Unwrap(left);
        right = this.Unwrap(right);

        switch (left, right)
        {
        // Never type is never reached, matches everything
        case (NeverType, _):
        case (_, NeverType):
        // Error type matches everything to avoid cascading type errors
        case (ErrorType, _):
        case (_, ErrorType):
        // Type variables could match anything
        case (TypeVariable, _):
        case (_, TypeVariable):
            return true;

        case (BuiltinType t1, BuiltinType t2):
            return t1.Name == t2.Name
                && t1.UnderylingType == t2.UnderylingType
                && t1.Bases.Any(x => t2.Bases.Select(x => x.Name).Contains(x.Name));

        case (FunctionType f1, FunctionType f2):
        {
            if (f1.ParameterTypes.Length != f2.ParameterTypes.Length) return false;
            for (var i = 0; i < f1.ParameterTypes.Length; ++i)
            {
                if (!this.Matches(f1.ParameterTypes[i], f2.ParameterTypes[i])) return false;
            }
            return this.Matches(f1.ReturnType, f2.ReturnType);
        }

        default:
            throw new System.NotImplementedException();
        }
    }

    private bool Unify(Type left, Type right)
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
        case (TypeVariable v, Type other):
        {
            this.Substitute(v, other);
            return true;
        }
        case (Type other, TypeVariable v):
        {
            this.Substitute(v, other);
            return true;
        }

        // Never type is never reached, unifies with everything
        case (NeverType, _):
        case (_, NeverType):
        // Error type unifies with everything to avoid cascading type errors
        case (ErrorType, _):
        case (_, ErrorType):
            return true;

        case (BuiltinType t1, BuiltinType t2):
            return t1.Name == t2.Name
                && t1.UnderylingType == t2.UnderylingType;

        case (FunctionType f1, FunctionType f2):
        {
            if (f1.ParameterTypes.Length != f2.ParameterTypes.Length) return false;
            for (var i = 0; i < f1.ParameterTypes.Length; ++i)
            {
                if (!this.Unify(f1.ParameterTypes[i], f2.ParameterTypes[i])) return false;
            }
            return this.Unify(f1.ReturnType, f2.ReturnType);
        }

        default:
            throw new System.NotImplementedException();
        }
    }

    private bool UnifyBase(Type left, Type right)
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
        case (TypeVariable v, Type other):
        {
            this.Substitute(v, other);
            return true;
        }
        case (Type other, TypeVariable v):
        {
            this.Substitute(v, other);
            return true;
        }

        // Never type is never reached, unifies with everything
        case (NeverType, _):
        case (_, NeverType):
        // Error type unifies with everything to avoid cascading type errors
        case (ErrorType, _):
        case (_, ErrorType):
            return true;

        case (BuiltinType t1, BuiltinType t2):
        {
            if (t1.IsBaseType) return t2.Bases.Select(x => x.Name).Contains(t1.Name);
            else if (t2.IsBaseType) return t1.Bases.Select(x => x.Name).Contains(t2.Name);
            else return t1.Name == t2.Name
                && t1.UnderylingType == t2.UnderylingType;
        }

        case (FunctionType f1, FunctionType f2):
        {
            if (f1.ParameterTypes.Length != f2.ParameterTypes.Length) return false;
            for (var i = 0; i < f1.ParameterTypes.Length; ++i)
            {
                if (!this.Unify(f1.ParameterTypes[i], f2.ParameterTypes[i])) return false;
            }
            return this.Unify(f1.ReturnType, f2.ReturnType);
        }

        default:
            throw new System.NotImplementedException();
        }
    }
}
