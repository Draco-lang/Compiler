using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.DebugAdapter;

internal sealed class PathEqualityComparer : EqualityComparer<string>
{
    public static PathEqualityComparer Instance { get; } = new();

    private PathEqualityComparer()
    {
    }

    public override int GetHashCode([DisallowNull] string obj) => Path.GetFullPath(obj).GetHashCode();
    public override bool Equals(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        return Path.GetFullPath(x).Equals(Path.GetFullPath(y));
    }
}
