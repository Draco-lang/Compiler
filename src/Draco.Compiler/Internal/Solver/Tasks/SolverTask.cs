using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Draco.Compiler.Internal.Solver;

namespace Draco.Compiler.Internal.Solver.Tasks;

internal static class SolverTask
{
    public static SolverTask<T> FromResult<T>(ConstraintSolver solver, T result)
    {
        var task = new SolverTask<T>();
        task.Awaiter.Solver = solver;
        task.Awaiter.SetResult(result, null);
        return task;
    }

    public static async SolverTask<ImmutableArray<T>> WhenAll<T>(IEnumerable<SolverTask<T>> tasks)
    {
        var result = ImmutableArray.CreateBuilder<T>();
        foreach (var task in tasks) result.Add(await task);
        return result.ToImmutable();
    }
}

[AsyncMethodBuilder(typeof(SolverTaskMethodBuilder<>))]
internal struct SolverTask<T>
{
    internal SolverTaskAwaiter<T> Awaiter;
    internal readonly ConstraintSolver Solver => this.Awaiter.Solver;
    public readonly bool IsCompleted => this.Awaiter.IsCompleted;
    public readonly T Result => this.Awaiter.GetResult();
    public readonly SolverTaskAwaiter<T> GetAwaiter() => this.Awaiter;
}
