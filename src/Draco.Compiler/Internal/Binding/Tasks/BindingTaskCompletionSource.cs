using System;
using Draco.Compiler.Internal.Solver;

namespace Draco.Compiler.Internal.Binding.Tasks;

internal sealed class BindingTaskCompletionSource<T>
{
    public BindingTask<T> Task
    {
        get
        {
            var task = new BindingTask<T>();
            task.Awaiter = this.Awaiter;
            return task;
        }
    }
    public bool IsCompleted => this.Awaiter.IsCompleted;
    public T Result => this.Awaiter.GetResult();

    internal BindingTaskAwaiter<T> Awaiter = new();

    public BindingTaskAwaiter<T> GetAwaiter() => this.Awaiter;
    public void SetResult(T result) => this.Awaiter.SetResult(result, null);
    public void SetException(Exception exception) => this.Awaiter.SetResult(default, exception);
}