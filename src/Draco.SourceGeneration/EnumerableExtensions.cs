using System;
using System.Collections.Generic;
using System.Text;

namespace Draco.SourceGeneration;

/// <summary>
/// Polyfill for ns2.0 things.
/// </summary>
internal static class EnumerableExtensions
{
    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> items, EqualityComparer<T>? comparer = null)
    {
        comparer ??= EqualityComparer<T>.Default;
        var result = new HashSet<T>(comparer);
        foreach (var item in items) result.Add(item);
        return result;
    }

    public static T MaxBy<T, U>(this IEnumerable<T> items, Func<T, U> selector, Comparer<U>? comparer = null)
    {
        comparer ??= Comparer<U>.Default;

        var enumerator = items.GetEnumerator();
        if (!enumerator.MoveNext()) throw new InvalidOperationException("sequence was empty");

        var maxElement = enumerator.Current;
        var maxValue = selector(maxElement);

        while (enumerator.MoveNext())
        {
            var element = enumerator.Current;
            var value = selector(element);
            if (comparer.Compare(maxValue, value) < 0)
            {
                maxElement = element;
                maxValue = value;
            }
        }

        return maxElement;
    }
}
