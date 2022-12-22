using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Utilities;

/// <summary>
/// Wraps an <see cref="IReadOnlyDictionary{TKey, TValue}"/> to be covariant on the value-type.
/// </summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
/// <typeparam name="TCovValue">The covariant value type.</typeparam>
/// <param name="Dictionary">The wrapped <see cref="IReadOnlyDictionary{TKey, TValue}"/>.</param>
internal readonly record struct CovariantReadOnlyDictionary<TKey, TValue, TCovValue>(
    IReadOnlyDictionary<TKey, TValue> Dictionary) : IReadOnlyDictionary<TKey, TCovValue>
    where TValue : TCovValue
{
    public TCovValue this[TKey key] => this.Dictionary[key];

    public int Count => this.Dictionary.Count;

    public IEnumerable<TKey> Keys => this.Dictionary.Keys;
    public IEnumerable<TCovValue> Values => this.Dictionary.Values.Cast<TCovValue>();

    public bool ContainsKey(TKey key) => this.Dictionary.ContainsKey(key);
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TCovValue value)
    {
        if (this.Dictionary.TryGetValue(key, out var covValue))
        {
            value = covValue;
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }

    public IEnumerator<KeyValuePair<TKey, TCovValue>> GetEnumerator() =>
        this.Dictionary.Select(kv => new KeyValuePair<TKey, TCovValue>(kv.Key, kv.Value)).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
