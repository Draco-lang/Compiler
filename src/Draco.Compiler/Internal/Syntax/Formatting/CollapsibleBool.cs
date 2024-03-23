using System;
using Draco.Compiler.Internal.Solver.Tasks;

namespace Draco.Compiler.Internal.Syntax.Formatting;

internal class CollapsibleBool : IEquatable<CollapsibleBool>
{
    private readonly SolverTaskCompletionSource<bool>? tcs;
    private readonly SolverTask<bool> task;

    private CollapsibleBool(SolverTaskCompletionSource<bool> tcs)
    {
        this.tcs = tcs;
        this.task = tcs.Task;
    }
    private CollapsibleBool(SolverTask<bool> task)
    {
        this.task = task;
    }

    public static CollapsibleBool Create() => new(new SolverTaskCompletionSource<bool>());
    public static CollapsibleBool Create(bool value) => new(SolverTask.FromResult(value));

    public void Collapse(bool collapse) => this.tcs?.SetResult(collapse);
    public bool TryCollapse(bool collapse)
    {
        if (!this.Collapsed.IsCompleted)
        {
            this.Collapse(collapse);
            return true;
        }
        return false;
    }
    public bool Equals(CollapsibleBool? other)
    {
        if (other is null) return false;

        if (this.tcs is null)
        {
            if (other.tcs is not null) return false;
            return this.task.Result == other.task.Result;
        }
        if (other.tcs is null) return false;
        if (this.tcs.IsCompleted && other.tcs.IsCompleted) return this.task.Result == other.task.Result;
        return false;
    }

    public override bool Equals(object? obj)
    {
        if (obj is CollapsibleBool collapsibleBool) return this.Equals(collapsibleBool);
        return false;
    }

    public SolverTask<bool> Collapsed => this.task;
}
