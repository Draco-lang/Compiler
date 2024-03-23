using System;
using Draco.Compiler.Internal.Solver.Tasks;

namespace Draco.Compiler.Internal.Syntax.Formatting;

internal class CollapsibleValue<T>
    where T : struct
{
    private readonly SolverTaskCompletionSource<T>? tcs;
    private readonly SolverTask<T> task;

    private CollapsibleValue(SolverTaskCompletionSource<T> tcs)
    {
        this.tcs = tcs;
        this.task = tcs.Task;
    }
    private CollapsibleValue(SolverTask<T> task)
    {
        this.task = task;
    }

    public static CollapsibleValue<T> Create() => new(new SolverTaskCompletionSource<T>());
    public static CollapsibleValue<T> Create(T value) => new(SolverTask.FromResult(value));

    public void Collapse(T collapse) => this.tcs?.SetResult(collapse);
    public bool TryCollapse(T collapse)
    {
        if (!this.Collapsed.IsCompleted)
        {
            this.Collapse(collapse);
            return true;
        }
        return false;
    }
    public bool Equals(CollapsibleValue<T>? other)
    {
        if (other is null) return false;

        if (this.tcs is null)
        {
            if (other.tcs is not null) return false;
            return this.task.Result.Equals(other.task.Result);
        }
        if (other.tcs is null) return false;
        if (this.tcs.IsCompleted && other.tcs.IsCompleted) return this.task.Result.Equals(other.task.Result);
        return false;
    }

    public override bool Equals(object? obj)
    {
        if (obj is CollapsibleBool collapsibleBool) return this.Equals(collapsibleBool);
        return false;
    }

    public SolverTask<T> Collapsed => this.task;
}
