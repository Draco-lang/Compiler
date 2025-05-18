using System;
using System.Collections.Generic;
using System.Linq;

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

    private readonly Dictionary<Type, List<Constraints.Constraint>> constraints = [];

    /// <summary>
    /// Adds a constraint to the store.
    /// </summary>
    /// <param name="constraint">The constraint to add.</param>
    public void Add(Constraints.Constraint constraint)
    {
        if (!this.constraints.TryGetValue(constraint.GetType(), out var list))
        {
            list = [];
            this.constraints.Add(constraint.GetType(), list);
        }
        list.Add(constraint);
    }
}
