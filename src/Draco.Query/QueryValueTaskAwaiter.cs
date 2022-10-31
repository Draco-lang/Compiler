using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Query;

/// <summary>
/// An awaiter for <see cref="QueryValueTask{T}"/>s.
/// </summary>
/// <typeparam name="T">The result type of the query computation.</typeparam>
public struct QueryValueTaskAwaiter<T> : INotifyCompletion, IIdentifiableQueryAwaiter
{
    public bool IsCompleted => this.yielded && (!this.isValueTask || (this.isValueTask && this.awaiter.IsCompleted));
    public int Identity { get; }

    private readonly bool isValueTask;
    private readonly T? result;
    private readonly ValueTaskAwaiter<T> awaiter;
    private bool yielded;

    internal QueryValueTaskAwaiter(T result, int queryIdentity)
    {
        this.isValueTask = false;
        this.awaiter = default;
        this.result = result;
        this.Identity = queryIdentity;
    }

    internal QueryValueTaskAwaiter(ValueTaskAwaiter<T> awaiter, int queryIdentity)
    {
        this.isValueTask = true;
        this.awaiter = awaiter;
        this.Identity = queryIdentity;
        this.result = default;
    }

    public void OnCompleted(Action continuation)
    {
        if (this.isValueTask)
        {
            this.awaiter.OnCompleted(continuation);
        }
        else
        {
            this.yielded = true;
            continuation(); // We are wrapping a result, so we can continue immediatly.
        }
    }

    public T GetResult() => this.isValueTask ? this.awaiter.GetResult() : this.result!;
}
