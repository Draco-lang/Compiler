using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Draco.Query;

/// <summary>
/// A type-map with an efficient way to look up elements.
/// </summary>
/// <typeparam name="TValue">The stored value type.</typeparam>
internal sealed class TypeMap<TValue>
{
    private static int typeIndex = -1;

    private static class TypeKey<TKey>
    {
        public static readonly int Index = Interlocked.Increment(ref typeIndex);
    }

    private readonly object lockObj = new();
    private readonly List<TValue> values = new();

    /// <summary>
    /// Retrieves or constructs a value in this map.
    /// </summary>
    /// <typeparam name="TKey">The key type to retrieve the associated value from.</typeparam>
    /// <param name="valueFactory">The factory function that constructs the value to be placed into the map
    /// in case it's not present under the key.</param>
    /// <returns>The value associated with <typeparamref name="TKey"/>.</returns>
    public TValue GetOrAdd<TKey>(Func<TValue> valueFactory)
    {
        var index = TypeKey<TKey>.Index;
        lock (this.lockObj)
        {
            if (index >= this.values.Count)
            {
                var value = valueFactory();
                this.values.Add(value);
                return value;
            }
            return this.values[index];
        }
    }
}
