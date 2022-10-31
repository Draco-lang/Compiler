using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Query;

/// <summary>
/// A task type for query computations.
/// </summary>
/// <typeparam name="T">The type of the query result.</typeparam>
[AsyncMethodBuilder(typeof(QueryValueTaskMethodBuilder<>))]
public readonly struct QueryValueTask<T>
{
    private readonly ValueTask<T> valueTask;
    private readonly string queryIdentity;
    private readonly T result;

    internal QueryValueTask(T result, string queryIdentity)
    {
        this.valueTask = default;
        this.result = result;
        this.queryIdentity = queryIdentity;
    }
    internal QueryValueTask(ValueTask<T> valueTask, string queryIdentity)
    {
        this.valueTask = valueTask;
        this.queryIdentity = queryIdentity;
        this.result = default!;
    }

    public QueryValueTaskAwaiter<T> GetAwaiter() => this.valueTask != default
        ? new(this.valueTask.GetAwaiter(), this.queryIdentity)
        : new(this.result, this.queryIdentity);
}
