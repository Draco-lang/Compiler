using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Types;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Solves type-constraint problems for the binder.
/// </summary>
internal sealed class ConstraintSolver
{
    /// <summary>
    /// Allocates a type-variable.
    /// </summary>
    public TypeVariable NextTypeVariable => new TypeVariable(this.typeVariableCounter++);
    private int typeVariableCounter = 0;

    private readonly List<Constraint> constraints = new();

    /// <summary>
    /// Solves all constraints within the solver.
    /// </summary>
    public void Solve() => throw new System.NotImplementedException();

    /// <summary>
    /// Adds an assignable constraint to the solver.
    /// </summary>
    /// <param name="targetType">The type being assigned to.</param>
    /// <param name="targetType">The type assigned.</param>
    /// <returns>The promise for the constraint added.</returns>
    public ConstraintSolverPromise<Unit> Assignable(Type targetType, Type assignedType)
    {
        // TODO: Hack, this is temporary until we have other constraints
        var constraint = new SameTypeConstraint(targetType, assignedType);
        this.constraints.Add(constraint);
        return new(default, constraint);
    }

    /// <summary>
    /// Adds a call constraint to the solver.
    /// </summary>
    /// <param name="functionType">The function type being called.</param>
    /// <param name="argTypes">The argument types that the function is called with.</param>
    /// <returns>The promise for the constraint added.</returns>
    public ConstraintSolverPromise<Type> Call(Type functionType, IEnumerable<Type> argTypes)
    {
        // We can save on type variables here
        var returnType = functionType is FunctionType f ? f.ReturnType : this.NextTypeVariable;
        // TODO: Hack, this is temporary until we have other constraints
        // Construct a type for the call-site
        var callSiteType = new FunctionType(argTypes.ToImmutableArray(), returnType);
        var constraint = new SameTypeConstraint(functionType, callSiteType);
        this.constraints.Add(constraint);
        return new(returnType, constraint);
    }
}
