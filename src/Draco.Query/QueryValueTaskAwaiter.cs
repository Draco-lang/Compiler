using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Query;
public readonly struct QueryValueTaskAwaiter<T> : INotifyCompletion
{
    private readonly bool isValueTask;
    private readonly T? result;
    private readonly ValueTaskAwaiter<T> awaiter;

    public QueryValueTaskAwaiter(T result)
    {
        this.isValueTask = false;
        this.awaiter = default;
        this.result = result;
    }

    public QueryValueTaskAwaiter(ValueTaskAwaiter<T> awaiter)
    {
        this.isValueTask = true;
        this.awaiter = awaiter;
        this.result = default;
    }

    public bool IsCompleted => !this.isValueTask || this.awaiter.IsCompleted;

    public void OnCompleted(Action continuation)
    {
        if (this.isValueTask)
        {
            this.awaiter.OnCompleted(continuation);
        }
        else
        {
            continuation(); // We are wrapping a result, so we can continue immediatly.
        }
    }

    public T GetResult() => this.isValueTask ? this.awaiter.GetResult() : this.result!;
}
