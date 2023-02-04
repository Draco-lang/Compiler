using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Utilities;

/// <summary>
/// A comparer for dictionaries that compares contents.
/// </summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
internal sealed class DictionaryEqualityComparer<TKey, TValue> :
    IEqualityComparer<IReadOnlyDictionary<TKey, TValue>>,
    IEqualityComparer<IDictionary<TKey, TValue>>
{
    /// <summary>
    /// A default equality comparer.
    /// </summary>
    public static DictionaryEqualityComparer<TKey, TValue> Default { get; } =
        new(EqualityComparer<TKey>.Default, EqualityComparer<TValue>.Default);

    /// <summary>
    /// The comparer for keys.
    /// </summary>
    public IEqualityComparer<TKey> KeyComparer { get; }

    /// <summary>
    /// The comparer for values.
    /// </summary>
    public IEqualityComparer<TValue> ValueComparer { get; }

    public DictionaryEqualityComparer(IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
    {
        this.KeyComparer = keyComparer;
        this.ValueComparer = valueComparer;
    }

    public bool Equals(IReadOnlyDictionary<TKey, TValue>? x, IReadOnlyDictionary<TKey, TValue>? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        if (x.Count != y.Count) return false;
        foreach (var (key, v1) in x)
        {
            if (!y.TryGetValue(key, out var v2)) return false;
            if (!this.ValueComparer.Equals(v1, v2)) return false;
        }
        return true;
    }
    public bool Equals(IDictionary<TKey, TValue>? x, IDictionary<TKey, TValue>? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        if (x.Count != y.Count) return false;
        foreach (var (key, v1) in x)
        {
            if (!y.TryGetValue(key, out var v2)) return false;
            if (!this.ValueComparer.Equals(v1, v2)) return false;
        }
        return true;
    }

    public int GetHashCode([DisallowNull] IReadOnlyDictionary<TKey, TValue> obj)
    {
        // We use XOR for order-independence
        var h = 0;
        foreach (var kv in obj) h ^= this.GetHashCode(kv);
        return h;
    }
    public int GetHashCode([DisallowNull] IDictionary<TKey, TValue> obj)
    {
        // We use XOR for order-independence
        var h = 0;
        foreach (var kv in obj) h ^= this.GetHashCode(kv);
        return h;
    }

    private int GetHashCode(KeyValuePair<TKey, TValue> obj) =>
        this.KeyComparer.GetHashCode(obj.Key!) * 31 + this.ValueComparer.GetHashCode(obj.Value!);
}
