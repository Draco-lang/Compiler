using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Query;

// NOTE: We eventually want to make this thread-safe.
/// <summary>
/// Manages memoized results of the compiler.
/// </summary>
internal sealed class QueryDatabase
{
    private readonly record struct ComputationKey(string Name, object Args);
    private readonly record struct Computation(ComputationKey Key, object Context);

    private readonly Dictionary<object, object> memoizedValues = new();
    private readonly List<Computation> computationStack = new();

    #region GetOrUpdate
    public TResult GetOrUpdate<T1, TResult>(
        T1 args,
        Func<T1, TResult> recompute,
        Func<T1, TResult>? handleCycle = null,
        [CallerMemberName] string queryName = "") =>
        this.GetOrUpdate(
            queryName: queryName,
            args: args,
            createContext: () => default(Unit),
            recompute: (_, a1) => recompute(a1),
            handleCycle: handleCycle is null ? null : (ctx, a1) => handleCycle(a1));
    public TResult GetOrUpdate<T1, TContext, TResult>(
        T1 args,
        Func<TContext> createContext,
        Func<TContext, T1, TResult> recompute,
        Func<TContext, T1, TResult>? handleCycle = null,
        [CallerMemberName] string queryName = "") =>
        this.GetOrUpdateImpl(
            queryName: queryName,
            args: args!,
            createContext: createContext,
            recompute: ctx => recompute(ctx, args),
            handleCycle: handleCycle is null ? null : ctx => handleCycle(ctx, args));

    public TResult GetOrUpdate<T1, T2, TResult>(
        (T1, T2) args,
        Func<T1, T2, TResult> recompute,
        Func<T1, T2, TResult>? handleCycle = null,
        [CallerMemberName] string queryName = "") =>
        this.GetOrUpdate(
            queryName: queryName,
            args: args,
            createContext: () => default(Unit),
            recompute: (_, a1, a2) => recompute(a1, a2),
            handleCycle: handleCycle is null ? null : (ctx, a1, a2) => handleCycle(a1, a2));
    public TResult GetOrUpdate<T1, T2, TContext, TResult>(
        (T1, T2) args,
        Func<TContext> createContext,
        Func<TContext, T1, T2, TResult> recompute,
        Func<TContext, T1, T2, TResult>? handleCycle = null,
        [CallerMemberName] string queryName = "") =>
        this.GetOrUpdateImpl(
            queryName: queryName,
            args: args!,
            createContext: createContext,
            recompute: ctx => recompute(ctx, args.Item1, args.Item2),
            handleCycle: handleCycle is null ? null : ctx => handleCycle(ctx, args.Item1, args.Item2));
    #endregion

    private TResult GetOrUpdateImpl<TContext, TResult>(
        string queryName,
        object args,
        Func<TContext> createContext,
        Func<TContext, TResult> recompute,
        Func<TContext, TResult>? handleCycle)
    {
        var computationKey = new ComputationKey(queryName, args);
        if (!this.memoizedValues.TryGetValue(computationKey, out var value))
        {
            // Check, if the key is present
            var indexOfKey = this.computationStack.FindIndex(c => c.Key == computationKey);
            if (indexOfKey >= 0)
            {
                // Present, cycle detected
                if (handleCycle is null)
                {
                    // The query does not handle cycles, error out
                    var calledQueries = this.computationStack
                        .Skip(indexOfKey)
                        .Select(c => c.Key)
                        .Append(computationKey);
                    var calledQueryNames = calledQueries.Select(k => $"{k.Name} [{k.Args}]");
                    var cycle = string.Join("\n", calledQueryNames.Select(q => $" * {q}"));
                    throw new InvalidOperationException($"Cycle detected:\n{cycle}");
                }
                else
                {
                    // This query can handle a cycle fine
                    var context = (TContext)this.computationStack[indexOfKey].Context;
                    return handleCycle(context);
                }
            }
            // Create context
            var ctx = createContext();
            // Push onto stack
            this.computationStack.Add(new(computationKey, ctx!));
            // Actually perform computation
            value = recompute(ctx);
            // Pop
            this.computationStack.RemoveAt(this.computationStack.Count - 1);
            // Memoize
            this.memoizedValues.Add(computationKey, value!);
        }
        return (TResult)value!;
    }
}
