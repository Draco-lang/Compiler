using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Solver.Tasks;

internal static class SolverTask
{
    public static SolverTask<T> FromResult<T>(T result)
    {
        var task = new SolverTask<T>();
        task.Awaiter.SetResult(result);
        return task;
    }

    public static async SolverTask<ImmutableArray<T>> WhenAll<T>(IEnumerable<SolverTask<T>> tasks)
    {
        var result = ImmutableArray.CreateBuilder<T>();
        foreach (var task in tasks) result.Add(await task);
        return result.ToImmutable();
    }

    public static SolverTask<Unit> WhenAny<T>(IEnumerable<SolverTask<T>> tasks)
    {
        if (tasks.Any(x => x.IsCompleted)) return FromResult<Unit>(default);
        var tcs = new SolverTaskCompletionSource<Unit>();
        foreach (var task in tasks)
        {
            task.Awaiter.OnCompleted(() => tcs.SetResult(default));
        }
        return tcs.Task;
    }
}

[AsyncMethodBuilder(typeof(SolverTaskMethodBuilder<>))]
internal sealed class SolverTask<T>
{
    internal SolverTaskAwaiter<T> Awaiter = new();
    public bool IsCompleted => this.Awaiter.IsCompleted;
    public T Result => this.Awaiter.GetResult();
    public SolverTaskAwaiter<T> GetAwaiter() => this.Awaiter;
}
