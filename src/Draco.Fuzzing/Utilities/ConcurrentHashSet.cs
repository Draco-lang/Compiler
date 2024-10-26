using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Draco.Fuzzing.Utilities;

/// <summary>
/// A concurrent hash set.
/// </summary>
/// <typeparam name="T">The type of the elements in the set.</typeparam>
/// <param name="comparer">The comparer to use for the elements.</param>
internal sealed class ConcurrentHashSet<T>(EqualityComparer<T>? comparer = null) : IReadOnlyCollection<T>
    where T : notnull
{
    public int Count => this.dict.Count;

    private readonly ConcurrentDictionary<T, byte> dict = new(comparer ?? EqualityComparer<T>.Default);

    /// <summary>
    /// Adds an item to the set.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <returns>True, if the item was new, and was added; otherwise, false.</returns>
    public bool Add(T item) => this.dict.TryAdd(item, 0);

    public IEnumerator<T> GetEnumerator() => this.dict.Keys.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
