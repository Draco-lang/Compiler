using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Solver;

internal sealed partial class ConstraintSolver
{
    private readonly Dictionary<TypeVariable, Type> substitutions = new(ReferenceEqualityComparer.Instance);

    private SolveState Solve(Constraint constraint) => constraint switch
    {
        SameTypeConstraint c => this.Solve(c),
        OverloadConstraint c => this.Solve(c),
        _ => throw new System.ArgumentOutOfRangeException(nameof(constraint)),
    };

    private SolveState Solve(SameTypeConstraint constraint)
    {
        throw new System.NotImplementedException();
    }

    private SolveState Solve(OverloadConstraint constraint)
    {
        throw new System.NotImplementedException();
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
