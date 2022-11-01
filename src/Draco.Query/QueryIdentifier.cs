using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Draco.Query;

/// <summary>
/// Used to identify query results.
/// </summary>
public readonly record struct QueryIdentifier
{
    private static int identityCounter = -1;

    /// <summary>
    /// An invalid <see cref="QueryIdentifier"/>.
    /// </summary>
    public static readonly QueryIdentifier Invalid = new(-1);

    /// <summary>
    /// Generates a new, unique <see cref="QueryIdentifier"/>.
    /// </summary>
    public static QueryIdentifier New => new(Interlocked.Increment(ref identityCounter));

    private readonly int id = -1;

    internal QueryIdentifier(int id)
    {
        this.id = id;
    }

    /// <inheritdoc/>
    public override string ToString() => $"Query[{this.id}]";
}
