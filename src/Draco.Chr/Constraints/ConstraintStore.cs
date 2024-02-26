using System.Collections;
using System.Collections.Generic;

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
    public bool Remove(IConstraint item) => this.constraints.Remove(item);
    public void Clear() => throw new System.NotImplementedException();
    public bool Contains(IConstraint item) => this.constraints.Contains(item);
    public void CopyTo(IConstraint[] array, int arrayIndex) => this.constraints.CopyTo(array, arrayIndex);
    public IEnumerator<IConstraint> GetEnumerator() => this.constraints.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
