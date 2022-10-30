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
    private readonly T result;

    public QueryValueTask(T result)
    {
        this.valueTask = default;
        this.result = result;
    }
    public QueryValueTask(ValueTask<T> valueTask)
    {
        this.valueTask = valueTask;
        this.result = default!;
    }

    public QueryValueTaskAwaiter<T> GetAwaiter() => this.valueTask != default
        ? new(this.valueTask.GetAwaiter())
        : new(this.result);
}
