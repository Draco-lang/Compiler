using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Utilities;

/// <summary>
/// Extensions for <see cref="IEnumerable{T}"/>s.
/// </summary>
internal static class EnumerableExtensions
{
    /// <summary>
    /// Checks if a given sequence is ordered in ascending order.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="vs">The sequence to check.</param>
    /// <param name="comparer">The comparer to compare items with.</param>
    /// <returns>True, if <paramref name="vs"/> are ordered in ascending order.</returns>
    public static bool IsOrdered<T>(this IEnumerable<T> vs, IComparer<T>? comparer = null)
    {
        comparer ??= Comparer<T>.Default;
        var enumerator = vs.GetEnumerator();
        // Empty sequences are considered sorted
        if (!enumerator.MoveNext()) return true;
        var prev = enumerator.Current;
        while (enumerator.MoveNext())
        {
            var curr = enumerator.Current;
            if (comparer.Compare(prev, curr) > 0) return false;
            prev = curr;
        }
        return true;
    }
}
