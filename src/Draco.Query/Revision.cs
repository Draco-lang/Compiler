using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Draco.Query;

/// <summary>
/// Represents a revision/version number in the system.
/// </summary>
public readonly record struct Revision : IComparable<Revision>
{
    private static int versionCounter = -1;

    /// <summary>
    /// An invalid revision number.
    /// </summary>
    public static readonly Revision Invalid = new(-1);

    /// <summary>
    /// The maximum possible revision number.
    /// </summary>
    public static readonly Revision MaxValue = new(int.MaxValue);

    /// <summary>
    /// Generates a new, unique <see cref="Revision"/>.
    /// </summary>
    public static Revision New => new(Interlocked.Increment(ref versionCounter));

    private readonly int version = -1;

    internal Revision(int version)
    {
        this.version = version;
    }

    /// <inheritdoc/>
    public override string ToString() => $"v{this.version}";

    /// <inheritdoc/>
    public int CompareTo(Revision other) => this.version - other.version;

    public static bool operator <(Revision a, Revision b) => a.CompareTo(b) < 0;
    public static bool operator >(Revision a, Revision b) => a.CompareTo(b) > 0;
    public static bool operator <=(Revision a, Revision b) => a.CompareTo(b) <= 0;
    public static bool operator >=(Revision a, Revision b) => a.CompareTo(b) >= 0;
}
