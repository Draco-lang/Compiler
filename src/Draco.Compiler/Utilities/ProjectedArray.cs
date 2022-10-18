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
/// Represents an immutable array that lazily projects elements from another array with a transformation
/// function.
/// </summary>
/// <typeparam name="TFrom">The item type of the source array.</typeparam>
/// <typeparam name="TTo">The item type of the </typeparam>
internal readonly struct ProjectedArray<TFrom, TTo> : IImmutableList<TTo>
{
    /// <summary>
    /// An empty <see cref="ProjectedArray{TFrom, TTo}"/>.
    /// </summary>
    public static readonly ProjectedArray<TFrom, TTo> Empty =
        new(ImmutableArray<TFrom>.Empty, _ => throw new InvalidOperationException());

    private readonly IImmutableList<TFrom> sourceValues;
    private readonly Func<TFrom, TTo> transformer;
    private readonly (TTo Value, bool Computed)[] projectedValues;

    public ProjectedArray(IImmutableList<TFrom> sourceValues, Func<TFrom, TTo> transformer)
    {
        this.sourceValues = sourceValues;
        this.transformer = transformer;
        this.projectedValues = new (TTo Value, bool Computed)[this.sourceValues.Count];
    }

    // Implementation of IImmutableList<T> /////////////////////////////////////

    /// <inheritdoc/>
    public TTo this[int index]
    {
        get
        {
            var (existing, computed) = this.projectedValues[index];
            if (computed) return existing;

            var projected = this.transformer(this.sourceValues[index]);
            this.projectedValues[index] = (projected, true);
            return projected;
        }
    }

    /// <inheritdoc/>
    public int Count => this.sourceValues.Count;

    /// <inheritdoc/>
    public IImmutableList<TTo> Add(TTo value) => throw new NotSupportedException();

    /// <inheritdoc/>
    public IImmutableList<TTo> AddRange(IEnumerable<TTo> items) => throw new NotSupportedException();

    /// <inheritdoc/>
    public IImmutableList<TTo> Clear() => throw new NotSupportedException();

    /// <inheritdoc/>
    public IEnumerator<TTo> GetEnumerator() => this.Enumerate().GetEnumerator();

    /// <inheritdoc/>
    public int IndexOf(TTo item, int index, int count, IEqualityComparer<TTo>? equalityComparer) =>
        throw new NotSupportedException();

    /// <inheritdoc/>
    public IImmutableList<TTo> Insert(int index, TTo element) => throw new NotSupportedException();

    /// <inheritdoc/>
    public IImmutableList<TTo> InsertRange(int index, IEnumerable<TTo> items) => throw new NotSupportedException();

    /// <inheritdoc/>
    public int LastIndexOf(TTo item, int index, int count, IEqualityComparer<TTo>? equalityComparer) =>
        throw new NotSupportedException();

    /// <inheritdoc/>
    public IImmutableList<TTo> Remove(TTo value, IEqualityComparer<TTo>? equalityComparer) =>
        throw new NotSupportedException();

    /// <inheritdoc/>
    public IImmutableList<TTo> RemoveAll(Predicate<TTo> match) => throw new NotSupportedException();

    /// <inheritdoc/>
    public IImmutableList<TTo> RemoveAt(int index) => throw new NotSupportedException();

    /// <inheritdoc/>
    public IImmutableList<TTo> RemoveRange(IEnumerable<TTo> items, IEqualityComparer<TTo>? equalityComparer) =>
        throw new NotSupportedException();

    /// <inheritdoc/>
    public IImmutableList<TTo> RemoveRange(int index, int count) => throw new NotSupportedException();

    /// <inheritdoc/>
    public IImmutableList<TTo> Replace(TTo oldValue, TTo newValue, IEqualityComparer<TTo>? equalityComparer) =>
        throw new NotSupportedException();

    /// <inheritdoc/>
    public IImmutableList<TTo> SetItem(int index, TTo value) => throw new NotSupportedException();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    private IEnumerable<TTo> Enumerate()
    {
        for (var i = 0; i < this.Count; ++i) yield return this[i];
    }
}

/// <summary>
/// Factory methods for <see cref="ProjectedArray{TFrom, TTo}"/>.
/// </summary>
internal static class ProjectedArray
{
    /// <summary>
    /// Construct a <see cref="ProjectedArray{TFrom, TTo}"/> from an <see cref="IImmutableList{T}"/>.
    /// </summary>
    /// <typeparam name="TFrom">The item tipe of the source array.</typeparam>
    /// <typeparam name="TTo">The projected item type.</typeparam>
    /// <param name="list">The source list to project the elements of.</param>
    /// <param name="projection">The projection function to be applied to each element.</param>
    /// <returns>A <see cref="ProjectedArray{TFrom, TTo}"/> that contains the elements of
    /// <paramref name="list"/> projected by <paramref name="projection"/>.</returns>
    public static ProjectedArray<TFrom, TTo> Project<TFrom, TTo>(
        this IImmutableList<TFrom> list,
        Func<TFrom, TTo> projection) => new(list, projection);
}
