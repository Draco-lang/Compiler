using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Draco.Compiler.Internal.Solver.Constraints;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// A data-type to efficiently store and query constraints.
/// </summary>
internal sealed class ConstraintStore
{
    /// <summary>
    /// The number of constraints in the store.
    /// </summary>
    public int Count => this.constraints.Values.Sum(list => list.Count);

    private readonly Dictionary<Type, List<Constraint>> constraints = [];

    /// <summary>
    /// Adds a constraint to the store.
    /// </summary>
    /// <param name="constraint">The constraint to add.</param>
    public void Add(Constraint constraint)
    {
        if (!this.constraints.TryGetValue(constraint.GetType(), out var list))
        {
            list = [];
            this.constraints.Add(constraint.GetType(), list);
        }
        list.Add(constraint);
    }

    /// <summary>
    /// Removes the first constraint of a given type.
    /// </summary>
    /// <typeparam name="TConstraint">The type of the constraint to remove.</typeparam>
    /// <param name="predicate">The predicate to match the constraint.</param>
    /// <param name="constraint">The constraint removed.</param>
    /// <returns>True if the constraint was found and removed, false otherwise.</returns>
    public bool TryRemove<TConstraint>([MaybeNullWhen(false)] out TConstraint constraint, Func<TConstraint, bool>? predicate = null)
        where TConstraint : Constraint
    {
        if (!this.constraints.TryGetValue(typeof(TConstraint), out var list))
        {
            constraint = null;
            return false;
        }
        if (list.Count == 0)
        {
            constraint = null;
            return false;
        }
        predicate ??= _ => true;
        for (var i = list.Count - 1; i >= 0; --i)
        {
            var constraintAtI = (TConstraint)list[i];
            if (!predicate(constraintAtI)) continue;

            list.RemoveAt(i);
            constraint = constraintAtI;
            return true;
        }
        constraint = null;
        return false;
    }
}
