using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Utilities;

/// <summary>
/// Factory methods for <see cref="ValueArray{T}"/>.
/// </summary>
internal static class ValueArray
{
    /// <summary>
    /// Builds a <see cref="ValueArray{T}"/> from <paramref name="items"/>.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">The items to create the array from.</param>
    /// <returns>The constructed array containing <paramref name="items"/>.</returns>
    public static ValueArray<T> Create<T>(params T[] items) =>
        new(ImmutableArray.Create(items));
}

/// <summary>
/// Represents an immutable array that is compared element-wise instead of by reference.
/// </summary>
/// <typeparam name="T">The element type of the array.</typeparam>
internal readonly struct ValueArray<T> : IEquatable<ValueArray<T>>, IImmutableList<T>
{
    /// <summary>
    /// An empty <see cref="ValueArray{T}"/>.
    /// </summary>
    public static readonly ValueArray<T> Empty = new(ImmutableArray<T>.Empty);

    private readonly IImmutableList<T> values;

    public ValueArray(IImmutableList<T> values)
    {
        this.values = values;
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj) => base.Equals(obj);

    /// <inheritdoc/>
    public bool Equals(ValueArray<T> other)
    {
        if (this.values.Count != other.values.Count) return false;
        for (var i = 0; i < this.values.Count; i++)
        {
            if (!Equals(this.values[i], other.values[i])) return false;
        }
        return true;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        foreach (var item in this.values) hash.Add(item);
        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    public override string ToString() => $"[{string.Join(", ", this.values)}]";

    // Implementation of IImmutableList<T> /////////////////////////////////////

    /// <inheritdoc/>
    public T this[int index] => ((IReadOnlyList<T>)this.values)[index];

    /// <inheritdoc/>
    public int Count => this.values.Count;

    /// <inheritdoc/>
    public IImmutableList<T> Add(T value) => this.values.Add(value);

    /// <inheritdoc/>
    public IImmutableList<T> AddRange(IEnumerable<T> items) => this.values.AddRange(items);

    /// <inheritdoc/>
    public IImmutableList<T> Clear() => this.values.Clear();

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator() => this.values.GetEnumerator();

    /// <inheritdoc/>
    public int IndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer) => this.values.IndexOf(item, index, count, equalityComparer);

    /// <inheritdoc/>
    public IImmutableList<T> Insert(int index, T element) => this.values.Insert(index, element);

    /// <inheritdoc/>
    public IImmutableList<T> InsertRange(int index, IEnumerable<T> items) => this.values.InsertRange(index, items);

    /// <inheritdoc/>
    public int LastIndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer) => this.values.LastIndexOf(item, index, count, equalityComparer);

    /// <inheritdoc/>
    public IImmutableList<T> Remove(T value, IEqualityComparer<T>? equalityComparer) => this.values.Remove(value, equalityComparer);

    /// <inheritdoc/>
    public IImmutableList<T> RemoveAll(Predicate<T> match) => this.values.RemoveAll(match);

    /// <inheritdoc/>
    public IImmutableList<T> RemoveAt(int index) => this.values.RemoveAt(index);

    /// <inheritdoc/>
    public IImmutableList<T> RemoveRange(IEnumerable<T> items, IEqualityComparer<T>? equalityComparer) => this.values.RemoveRange(items, equalityComparer);

    /// <inheritdoc/>
    public IImmutableList<T> RemoveRange(int index, int count) => this.values.RemoveRange(index, count);

    /// <inheritdoc/>
    public IImmutableList<T> Replace(T oldValue, T newValue, IEqualityComparer<T>? equalityComparer) => this.values.Replace(oldValue, newValue, equalityComparer);

    /// <inheritdoc/>
    public IImmutableList<T> SetItem(int index, T value) => this.values.SetItem(index, value);

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this.values).GetEnumerator();
}
