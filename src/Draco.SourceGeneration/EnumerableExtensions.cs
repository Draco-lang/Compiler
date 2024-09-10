using System;
using System.Collections.Generic;

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

    public static IEnumerable<(T, U)> Zip<T, U>(this IEnumerable<T> first, IEnumerable<U> second)
    {
        using var firstEnumerator = first.GetEnumerator();
        using var secondEnumerator = second.GetEnumerator();
        while (firstEnumerator.MoveNext() && secondEnumerator.MoveNext())
        {
            yield return (firstEnumerator.Current, secondEnumerator.Current);
        }
    }
}
