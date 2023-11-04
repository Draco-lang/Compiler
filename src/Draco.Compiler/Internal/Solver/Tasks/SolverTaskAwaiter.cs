using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Draco.Compiler.Internal.Binding.Tasks;
using Draco.Compiler.Internal.Solver;

namespace Draco.Compiler.Internal.Solver.Tasks;

internal struct SolverTaskAwaiter<T> : INotifyCompletion
{
    public bool IsCompleted { get; private set; }

    private T? result;
    private Exception? exception;
    private List<Action>? completions;

    internal void SetResult(T? result, Exception? exception)
    {
        this.IsCompleted = true;
        this.result = result;
        this.exception = exception;
        foreach (var completion in this.completions ?? Enumerable.Empty<Action>())
        {
            completion();
        }
    }

    public readonly T GetResult()
    {
        if (this.exception is not null) throw this.exception;
        return this.result!;
    }

    public void OnCompleted(Action completion)
    {
        if (this.IsCompleted)
        {
            completion();
        }
        else
        {
            this.completions ??= new();
            this.completions.Add(completion);
        }
    }
}
