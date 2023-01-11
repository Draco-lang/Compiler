using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Draco.Compiler.Internal.Utilities;

internal static class KeyValuePairExtensions
{
    public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kv, out TKey key, out TValue value)
    {
        key = kv.Key;
        value = kv.Value;
    }
}

internal static partial class EnumerableExtensions
{
    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> values, IEqualityComparer<T>? comparer = null)
    {
        comparer ??= EqualityComparer<T>.Default;
        return new HashSet<T>(values, comparer);
    }

    public static IEnumerable<TSource> SkipLast<TSource>(this IEnumerable<TSource> source, int count) => count <= 0
        ? source
        : SkipLastImpl(source, count);

    private static IEnumerable<TSource> SkipLastImpl<TSource>(IEnumerable<TSource> source, int count)
    {
        var backBuffer = new TSource[count];
        var index = 0;
        foreach (var item in source)
        {
            var swapIndex = (index + count) % count;
            var oldItem = backBuffer[swapIndex];
            backBuffer[swapIndex] = item;
            if (index >= count) yield return oldItem;
            ++index;
        }
    }

    public static TSource? MinBy<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        IComparer<TKey>? comparer = null)
    {
        comparer ??= Comparer<TKey>.Default;
        var enumerator = source.GetEnumerator();

        if (!enumerator.MoveNext())
        {
            if (default(TSource) is null) return default;
            else throw new InvalidOperationException("sequence contains no elements");
        }

        var minValue = enumerator.Current;
        var minKey = keySelector(minValue);

        while (enumerator.MoveNext())
        {
            var value = enumerator.Current;
            var key = keySelector(value);
            if (comparer.Compare(key, minKey) < 0)
            {
                minValue = value;
                minKey = key;
            }
        }

        return minValue;
    }
}

internal static class StackExtensions
{
    public static bool TryPop<T>(this Stack<T> stack, [MaybeNullWhen(false)] out T result)
    {
        if (stack.Count > 0)
        {
            result = stack.Pop();
            return true;
        }
        else
        {
            result = default;
            return false;
        }
    }

    public static bool TryPeek<T>(this Stack<T> stack, [MaybeNullWhen(false)] out T result)
    {
        if (stack.Count > 0)
        {
            result = stack.Peek();
            return true;
        }
        else
        {
            result = default;
            return false;
        }
    }
}

internal static class QueueExtensions
{
    public static bool TryDequeue<T>(this Queue<T> queue, [MaybeNullWhen(false)] out T result)
    {
        if (queue.Count > 0)
        {
            result = queue.Dequeue();
            return true;
        }
        else
        {
            result = default;
            return false;
        }
    }
}

internal static class StringBuilderExtensions
{
    public static StringBuilder AppendJoin<T>(this StringBuilder builder, string? separator, IEnumerable<T> values)
    {
        var enumerator = values.GetEnumerator();
        if (enumerator.MoveNext())
        {
            builder.Append(enumerator.Current);
            while (enumerator.MoveNext())
            {
                builder.Append(separator);
                builder.Append(enumerator.Current);
            }
        }
        return builder;
    }
}

// Source: https://source.dot.net/#System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/ReferenceEqualityComparer.cs
internal sealed class ReferenceEqualityComparer : IEqualityComparer<object?>, IEqualityComparer
{
    private ReferenceEqualityComparer() { }

    public static ReferenceEqualityComparer Instance { get; } = new ReferenceEqualityComparer();

    public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

    public int GetHashCode(object? obj) => RuntimeHelpers.GetHashCode(obj!);
}
