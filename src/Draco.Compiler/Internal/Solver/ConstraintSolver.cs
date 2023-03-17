using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Types;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Solves type-constraint problems for the binder.
/// </summary>
internal sealed partial class ConstraintSolver
{
    private enum SolveState
    {
        Stale,
        Progressing,
        Finished,
    }

    /// <summary>
    /// Allocates a type-variable.
    /// </summary>
    public TypeVariable NextTypeVariable => new(this.typeVariableCounter++);
    private int typeVariableCounter = 0;

    private readonly List<Constraint> constraints = new();

    /// <summary>
    /// Solves all constraints within the solver.
    /// </summary>
    public void Solve()
    {
        while (true)
        {
            var advanced = false;
            for (var i = 0; i < this.constraints.Count;)
            {
                var state = this.Solve(this.constraints[i]);
                advanced = advanced || state != SolveState.Stale;

                if (state == SolveState.Finished) this.constraints.RemoveAt(i);
                else ++i;
            }
            if (!advanced) break;
        }
        if (this.constraints.Count > 0)
        {
            // TODO: Didn't solve all constraints
            throw new System.InvalidOperationException();
        }
    }

    /// <summary>
    /// Adds a same-type constraint to the solver.
    /// </summary>
    /// <param name="first">The type that is constrained to be the same as <paramref name="second"/>.</param>
    /// <param name="second">The type that is constrained to be the same as <paramref name="first"/>.</param>
    /// <returns>The promise for the constraint added.</returns>
    public ConstraintPromise<Type> SameType(Type first, Type second)
    {
        var constraint = new SameTypeConstraint(first, second);
        this.constraints.Add(constraint);
        return constraint.Promise;
    }

    /// <summary>
    /// Adds an assignable constraint to the solver.
    /// </summary>
    /// <param name="targetType">The type being assigned to.</param>
    /// <param name="targetType">The type assigned.</param>
    /// <returns>The promise for the constraint added.</returns>
    public ConstraintPromise<Type> Assignable(Type targetType, Type assignedType)
    {
        // TODO: Hack, this is temporary until we have other constraints
        var constraint = new SameTypeConstraint(targetType, assignedType);
        this.constraints.Add(constraint);
        return constraint.Promise;
    }

    /// <summary>
    /// Adds a call constraint to the solver.
    /// </summary>
    /// <param name="functionType">The function type being called.</param>
    /// <param name="argTypes">The argument types that the function is called with.</param>
    /// <returns>The promise for the constraint added, containing the return type.</returns>
    public ConstraintPromise<Type> Call(Type functionType, IEnumerable<Type> argTypes)
    {
        // We can save on type variables here
        var returnType = functionType is FunctionType f ? f.ReturnType : this.NextTypeVariable;
        // TODO: Hack, this is temporary until we have other constraints
        // Construct a type for the call-site
        var callSiteType = new FunctionType(argTypes.ToImmutableArray(), returnType);
        // TODO: We disregard this promise
        // We need some promise mapping utility...
        var constraint = new SameTypeConstraint(functionType, callSiteType);
        this.constraints.Add(constraint);
        return constraint.Promise.Map(_ => returnType);
    }

    /// <summary>
    /// Adds an overload constraint to the solver.
    /// </summary>
    /// <param name="functions">The list of functions to choose an overload from.</param>
    /// <returns>The promise for the constraint added along with the call-site type.</returns>
    public (ConstraintPromise<FunctionSymbol> Symbol, Type CallSite) Overload(IEnumerable<FunctionSymbol> functions)
    {
        var callSite = this.NextTypeVariable;
        var constraint = new OverloadConstraint(functions, callSite);
        this.constraints.Add(constraint);
        return (constraint.Promise, constraint.CallSite);
    }
}
