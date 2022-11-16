using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Query.Tasks;

/// <summary>
/// An awaiter for <see cref="QueryValueTask{T}"/>s.
/// </summary>
/// <typeparam name="T">The result type of the query computation.</typeparam>
public struct QueryValueTaskAwaiter<T> : INotifyCompletion, IIdentifiableQueryAwaiter
{
    public bool IsCompleted => this.yielded && (!this.isValueTask || this.isValueTask && this.awaiter.IsCompleted);
    public QueryIdentifier Identity { get; }

    private readonly bool isValueTask;
    private readonly T? result;
    private readonly ValueTaskAwaiter<T> awaiter;
    private bool yielded;

    internal QueryValueTaskAwaiter(T result, QueryIdentifier identity)
    {
        this.isValueTask = false;
        this.awaiter = default;
        this.result = result;
        this.Identity = identity;
    }

    internal QueryValueTaskAwaiter(ValueTaskAwaiter<T> awaiter, QueryIdentifier identity)
    {
        this.isValueTask = true;
        this.awaiter = awaiter;
        this.Identity = identity;
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
            // We are wrapping a result, so we can continue immediatly
            continuation();
        }
    }

    public T GetResult() => this.isValueTask ? this.awaiter.GetResult() : this.result!;
}
