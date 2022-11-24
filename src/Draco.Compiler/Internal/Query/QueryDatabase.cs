using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Query;

/// <summary>
/// Manages memoized results of the compiler.
/// </summary>
internal sealed class QueryDatabase
{
    private readonly Dictionary<object, object> memoizedValues = new();
    private readonly List<(string Name, object Args)> computationStack = new();

    #region GetOrUpdate
    public TResult GetOrUpdate<T1, TResult>(
        T1 args,
        Func<T1, TResult> compute,
        [CallerMemberName] string queryName = "") =>
        this.GetOrUpdateImpl(queryName, args!, () => compute(args));
    public TResult GetOrUpdate<T1, T2, TResult>(
        (T1, T2) args,
        Func<T1, T2, TResult> compute,
        [CallerMemberName] string queryName = "") =>
        this.GetOrUpdateImpl(queryName, args, () => compute(args.Item1, args.Item2));
    public TResult GetOrUpdate<T1, T2, T3, TResult>(
        (T1, T2, T3) args,
        Func<T1, T2, T3, TResult> compute,
        [CallerMemberName] string queryName = "") =>
        this.GetOrUpdateImpl(queryName, args, () => compute(args.Item1, args.Item2, args.Item3));
    public TResult GetOrUpdate<T1, T2, T3, T4, TResult>(
        (T1, T2, T3, T4) args,
        Func<T1, T2, T3, T4, TResult> compute,
        [CallerMemberName] string queryName = "") =>
        this.GetOrUpdateImpl(queryName, args, () => compute(args.Item1, args.Item2, args.Item3, args.Item4));
    public TResult GetOrUpdate<T1, T2, T3, T4, T5, TResult>(
        (T1, T2, T3, T4, T5) args,
        Func<T1, T2, T3, T4, T5, TResult> compute,
        [CallerMemberName] string queryName = "") =>
        this.GetOrUpdateImpl(queryName, args, () => compute(args.Item1, args.Item2, args.Item3, args.Item4, args.Item5));
    public TResult GetOrUpdate<T1, T2, T3, T4, T5, T6, TResult>(
        (T1, T2, T3, T4, T5, T6) args,
        Func<T1, T2, T3, T4, T5, T6, TResult> compute,
        [CallerMemberName] string queryName = "") =>
        this.GetOrUpdateImpl(queryName, args, () => compute(args.Item1, args.Item2, args.Item3, args.Item4, args.Item5, args.Item6));
    public TResult GetOrUpdate<T1, T2, T3, T4, T5, T6, T7, TResult>(
        (T1, T2, T3, T4, T5, T6, T7) args,
        Func<T1, T2, T3, T4, T5, T6, T7, TResult> compute,
        [CallerMemberName] string queryName = "") =>
        this.GetOrUpdateImpl(queryName, args, () => compute(args.Item1, args.Item2, args.Item3, args.Item4, args.Item5, args.Item6, args.Item7));
    public TResult GetOrUpdate<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(
        (T1, T2, T3, T4, T5, T6, T7, T8) args,
        Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> compute,
        [CallerMemberName] string queryName = "") =>
        this.GetOrUpdateImpl(queryName, args, () => compute(args.Item1, args.Item2, args.Item3, args.Item4, args.Item5, args.Item6, args.Item7, args.Item8));
    public TResult GetOrUpdate<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(
        (T1, T2, T3, T4, T5, T6, T7, T8, T9) args,
        Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> compute,
        [CallerMemberName] string queryName = "") =>
        this.GetOrUpdateImpl(queryName, args, () => compute(args.Item1, args.Item2, args.Item3, args.Item4, args.Item5, args.Item6, args.Item7, args.Item8, args.Item9));
    public TResult GetOrUpdate<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(
        (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10) args,
        Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> compute,
        [CallerMemberName] string queryName = "") =>
        this.GetOrUpdateImpl(queryName, args, () => compute(args.Item1, args.Item2, args.Item3, args.Item4, args.Item5, args.Item6, args.Item7, args.Item8, args.Item9, args.Item10));
    public TResult GetOrUpdate<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(
        (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11) args,
        Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> compute,
        [CallerMemberName] string queryName = "") =>
        this.GetOrUpdateImpl(queryName, args, () => compute(args.Item1, args.Item2, args.Item3, args.Item4, args.Item5, args.Item6, args.Item7, args.Item8, args.Item9, args.Item10, args.Item11));
    public TResult GetOrUpdate<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(
        (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12) args,
        Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> compute,
        [CallerMemberName] string queryName = "") =>
        this.GetOrUpdateImpl(queryName, args, () => compute(args.Item1, args.Item2, args.Item3, args.Item4, args.Item5, args.Item6, args.Item7, args.Item8, args.Item9, args.Item10, args.Item11, args.Item12));
    public TResult GetOrUpdate<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(
        (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13) args,
        Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> compute,
        [CallerMemberName] string queryName = "") =>
        this.GetOrUpdateImpl(queryName, args, () => compute(args.Item1, args.Item2, args.Item3, args.Item4, args.Item5, args.Item6, args.Item7, args.Item8, args.Item9, args.Item10, args.Item11, args.Item12, args.Item13));
    public TResult GetOrUpdate<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(
        (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14) args,
        Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> compute,
        [CallerMemberName] string queryName = "") =>
        this.GetOrUpdateImpl(queryName, args, () => compute(args.Item1, args.Item2, args.Item3, args.Item4, args.Item5, args.Item6, args.Item7, args.Item8, args.Item9, args.Item10, args.Item11, args.Item12, args.Item13, args.Item14));
    public TResult GetOrUpdate<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(
        (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15) args,
        Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> compute,
        [CallerMemberName] string queryName = "") =>
        this.GetOrUpdateImpl(queryName, args, () => compute(args.Item1, args.Item2, args.Item3, args.Item4, args.Item5, args.Item6, args.Item7, args.Item8, args.Item9, args.Item10, args.Item11, args.Item12, args.Item13, args.Item14, args.Item15));
    public TResult GetOrUpdate<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>(
        (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16) args,
        Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> compute,
        [CallerMemberName] string queryName = "") =>
        this.GetOrUpdateImpl(queryName, args, () => compute(args.Item1, args.Item2, args.Item3, args.Item4, args.Item5, args.Item6, args.Item7, args.Item8, args.Item9, args.Item10, args.Item11, args.Item12, args.Item13, args.Item14, args.Item15, args.Item16));
    #endregion

    private T GetOrUpdateImpl<T>(string query, object args, Func<T> compute)
    {
        if (!this.memoizedValues.TryGetValue((query, args), out var value))
        {
            var computationKey = (query, args);
            // Check, if the key is present
            var indexOfKey = this.computationStack.IndexOf(computationKey);
            if (indexOfKey >= 0)
            {
                // Present, cycle detected
                var calledQueries = this.computationStack.Skip(indexOfKey);
                throw new InvalidOperationException($"Cycle detected:\n{string.Join("\n", calledQueries.Select(q => $"    {q}"))}");
            }
            // Push onto stack
            this.computationStack.Add(computationKey);
            // Actually perform computation
            value = compute();
            // Pop
            this.computationStack.RemoveAt(this.computationStack.Count - 1);
            // Memoize
            this.memoizedValues.Add((query, args), value!);
        }
        return (T)value!;
    }
}
