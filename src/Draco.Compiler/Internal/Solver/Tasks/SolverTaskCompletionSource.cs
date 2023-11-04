using System;
using Draco.Compiler.Internal.Solver;

namespace Draco.Compiler.Internal.Solver.Tasks;

internal sealed class SolverTaskCompletionSource<T>
{
    public SolverTask<T> Task
    {
        get
        {
            var task = new SolverTask<T>();
            task.Awaiter = this.Awaiter;
            return task;
        }
    }
    public bool IsCompleted => this.Awaiter.IsCompleted;
    public T Result => this.Awaiter.GetResult();

    internal SolverTaskAwaiter<T> Awaiter;

    public SolverTaskAwaiter<T> GetAwaiter() => this.Awaiter;
    public void SetResult(T result) => this.Awaiter.SetResult(result, null);
    public void SetException(Exception exception) => this.Awaiter.SetResult(default, exception);
}
