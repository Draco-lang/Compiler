using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Query.Tasks;

namespace Draco.Query;

/// <summary>
/// Any query result.
/// </summary>
internal interface IQueryResult
{
    /// <summary>
    /// The identifier of this result.
    /// </summary>
    public QueryIdentifier Identifier { get; }

    /// <summary>
    /// The revision where the result has last changed.
    /// </summary>
    public Revision ChangedAt { get; }

    /// <summary>
    /// The revision where the result has last been verified to be reusable.
    /// </summary>
    public Revision VerifiedAt { get; }

    /// <summary>
    /// The dependencies of this result.
    /// </summary>
    public ICollection<IQueryResult> Dependencies { get; }

    /// <summary>
    /// Refreshes this result, recomputing it if needed.
    /// </summary>
    public Task Refresh();
}

/// <summary>
/// A query result that is a fundamental input to the system.
/// </summary>
/// <typeparam name="T">The type of the input value.</typeparam>
internal sealed class InputQueryResult<T> : IQueryResult
{
    public QueryIdentifier Identifier { get; }
    public Revision ChangedAt { get; set; } = Revision.Invalid;
    public Revision VerifiedAt => Revision.MaxValue;
    public ICollection<IQueryResult> Dependencies => Array.Empty<IQueryResult>();
    public T Value { get; set; } = default!;

    public InputQueryResult(QueryIdentifier identifier)
    {
        this.Identifier = identifier;
    }

    public Task Refresh() => Task.CompletedTask;
}

/// <summary>
/// A query result that came from computation.
/// </summary>
/// <typeparam name="T">The type of the computed value.</typeparam>
internal sealed class ComputedQueryResult<T> : IQueryResult
{
    public QueryIdentifier Identifier { get; }
    public Revision ChangedAt { get; set; } = Revision.Invalid;
    public Revision VerifiedAt { get; set; } = Revision.Invalid;
    public ICollection<IQueryResult> Dependencies { get; } = new HashSet<IQueryResult>();
    public T Value { get; set; } = default!;

    public ComputedQueryResult(QueryIdentifier identifier)
    {
        this.Identifier = identifier;
    }

    public async Task Refresh() =>
        await QueryValueTaskMethodBuilder<T>.RunQueryByIdentifier(this.Identifier);
}
