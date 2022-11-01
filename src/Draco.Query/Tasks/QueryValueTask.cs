using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Query.Tasks;

/// <summary>
/// A task type for query computations.
/// </summary>
/// <typeparam name="T">The type of the query result.</typeparam>
[AsyncMethodBuilder(typeof(QueryValueTaskMethodBuilder<>))]
public readonly struct QueryValueTask<T>
{
    private readonly ValueTask<T> valueTask;
    private readonly QueryIdentifier identifier;
    private readonly T result;

    internal QueryValueTask(T result, QueryIdentifier identifier)
    {
        this.valueTask = default;
        this.result = result;
        this.identifier = identifier;
    }
    internal QueryValueTask(ValueTask<T> valueTask, QueryIdentifier identifier)
    {
        this.valueTask = valueTask;
        this.identifier = identifier;
        this.result = default!;
    }

    public QueryValueTaskAwaiter<T> GetAwaiter() => this.valueTask != default
        ? new(this.valueTask.GetAwaiter(), this.identifier)
        : new(this.result, this.identifier);
}
