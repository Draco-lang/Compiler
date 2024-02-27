using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Draco.Chr.Constraints;

/// <summary>
/// Responsible for storing constraints for the solver.
/// </summary>
public sealed class ConstraintStore : ICollection<IConstraint>
{
    public int Count => this.constraints.Count;
    public bool IsReadOnly => false;

    private readonly HashSet<IConstraint> constraints = [];

    public void Add(IConstraint item) => this.constraints.Add(item);
    public void AddRange(IEnumerable<IConstraint> items) => this.constraints.UnionWith(items);
    public void RemoveRange(IEnumerable<IConstraint> items) => this.constraints.ExceptWith(items);
    public bool Remove(IConstraint item) => this.constraints.Remove(item);
    public void Clear() => this.constraints.Clear();
    public bool Contains(IConstraint item) => this.constraints.Contains(item);
    public void CopyTo(IConstraint[] array, int arrayIndex) => this.constraints.CopyTo(array, arrayIndex);
    public IEnumerator<IConstraint> GetEnumerator() => this.constraints.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    public IEnumerable<IConstraint> ConstraintsOfType(Type type) =>
        this.constraints.Where(c => c.IsOfType(type));
    public IEnumerable<IConstraint> ConstraintsOfValue(object value) =>
        this.constraints.Where(c => Equals(c.Value, value));
}
