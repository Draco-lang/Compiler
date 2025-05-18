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
    public void Add(Constraint constraint) =>
        this.GetConstraintList(constraint.GetType()).Add(constraint);

    /// <summary>
    /// Gets the first constraint of a given type.
    /// </summary>
    /// <typeparam name="TConstraint">The type of the constraint to get.</typeparam>
    /// <param name="constraint">The constraint found.</param>
    /// <param name="predicate">The predicate to match the constraint.</param>
    /// <returns>True if the constraint was found, false otherwise.</returns>
    public bool TryGet<TConstraint>([MaybeNullWhen(false)] out TConstraint constraint, Func<TConstraint, bool>? predicate = null)
        where TConstraint : Constraint
    {
        var list = this.GetConstraintList(typeof(TConstraint));
        if (list.Count == 0)
        {
            constraint = null;
            return false;
        }
        predicate ??= _ => true;
        foreach (var c in list.Cast<TConstraint>())
        {
            if (!predicate(c)) continue;
            constraint = c;
            return true;
        }
        constraint = null;
        return false;
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
        var list = this.GetConstraintList(typeof(TConstraint));
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

    /// <summary>
    /// Queries the store for all constraints of a given type.
    /// </summary>
    /// <typeparam name="TConstraint">The type of the constraint to query.</typeparam>
    /// <param name="predicate">The predicate to match the constraint.</param>
    /// <returns>The constraints found.</returns>
    public IEnumerable<TConstraint> Query<TConstraint>(Func<TConstraint, bool>? predicate = null)
        where TConstraint : Constraint
    {
        var list = this.GetConstraintList(typeof(TConstraint));
        if (list.Count == 0) yield break;
        predicate ??= _ => true;
        foreach (var c in list.Cast<TConstraint>())
        {
            if (!predicate(c)) continue;
            yield return c;
        }
    }

    /// <summary>
    /// Removes all constraints in the given sequence.
    /// </summary>
    /// <typeparam name="TConstraint">The type of the constraint to remove.</typeparam>
    /// <param name="constraints">The constraints to remove.</param>
    public void RemoveAll<TConstraint>(IEnumerable<TConstraint> constraints)
        where TConstraint : Constraint
    {
        var list = this.GetConstraintList(typeof(TConstraint));
        foreach (var constraint in constraints)
        {
            if (!list.Remove(constraint)) throw new InvalidOperationException($"Constraint {constraint} not found in the store.");
        }
    }

    private List<Constraint> GetConstraintList(Type type)
    {
        if (!this.constraints.TryGetValue(type, out var list))
        {
            list = [];
            this.constraints.Add(type, list);
        }
        return list;
    }
}
