using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Utilities;

/// <summary>
/// Represents an immutable array that is compared element-wise instead of by reference.
/// </summary>
/// <typeparam name="T">The element type of the array.</typeparam>
internal readonly struct ValueArray<T> : IEquatable<ValueArray<T>>
{
    private readonly IReadOnlyList<T>? values;

    public ValueArray(IReadOnlyList<T> values)
    {
        this.values = values;
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj) => base.Equals(obj);

    /// <inheritdoc/>
    public bool Equals(ValueArray<T> other)
    {
        if (ReferenceEquals(this.values, other.values)) return true;
        if (this.values is null || other.values is null) return false;

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
        if (this.values is null) return HashCode.Combine((object?)null);

        var hash = default(HashCode);
        foreach (var item in this.values) hash.Add(item);
        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    public override string ToString() => $"[{string.Join(", ", this.values ?? Enumerable.Empty<T>())}]";
}
