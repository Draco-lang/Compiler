using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Solver;

internal sealed partial class ConstraintSolver
{
    private readonly Dictionary<TypeVariable, Type> substitutions = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<Type, Symbol> resolvedOverloads = new(ReferenceEqualityComparer.Instance);

    private SolveState Solve(Constraint constraint) => constraint switch
    {
        SameTypeConstraint c => this.Solve(c),
        OverloadConstraint c => this.Solve(c),
        _ => throw new System.ArgumentOutOfRangeException(nameof(constraint)),
    };

    private SolveState Solve(SameTypeConstraint constraint)
    {
        if (!this.Unify(constraint.First, constraint.Second))
        {
            // TODO: Fill out error
            throw new System.NotImplementedException();
        }
        return SolveState.Finished;
    }

    private SolveState Solve(OverloadConstraint constraint)
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
            // TODO: Fill out error
            throw new System.NotImplementedException();
            return SolveState.Finished;
        }
        // Ok solve
        if (constraint.Candidates.Count == 1)
        {
            this.Unify(constraint.Candidates[0].Type, constraint.CallSite);
            this.resolvedOverloads.Add(constraint.CallSite, constraint.Candidates[0]);
            return SolveState.Finished;
        }
        // Depends if we removed anything
        return advanced ? SolveState.Progressing : SolveState.Stale;
    }

    private bool Matches(Type left, Type right)
    {
        left = this.Unwrap(left);
        right = this.Unwrap(right);

        switch (left, right)
        {
        case (TypeVariable, _):
        case (_, TypeVariable):
            return true;

        case (BuiltinType t1, BuiltinType t2):
            return t1.Name == t2.Name
                && t1.UnderylingType == t2.UnderylingType;

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

    private void Substitute(TypeVariable typeVar, Type type) =>
        this.substitutions.Add(typeVar, type);

    private Type Unwrap(Type type)
    {
        // If not a type-variable, we consider it substituted
        if (type is not TypeVariable typeVar) return type;
        // If it is, but has no substitutions, just return it as-is
        if (!this.substitutions.TryGetValue(typeVar, out var substitution)) return typeVar;
        // If the substitution is also a type-variable, we prune
        if (substitution is TypeVariable)
        {
            substitution = this.Unwrap(substitution);
            this.substitutions[typeVar] = substitution;
        }
        return substitution;
    }
}
