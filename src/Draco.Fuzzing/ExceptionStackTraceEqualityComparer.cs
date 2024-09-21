using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Fuzzing;

/// <summary>
/// An equality comparer for exceptions that compares their stack traces.
/// </summary>
internal sealed class ExceptionStackTraceEqualityComparer : IEqualityComparer<Exception>
{
    public static ExceptionStackTraceEqualityComparer Instance { get; } = new();

    private ExceptionStackTraceEqualityComparer()
    {
    }

    public bool Equals(Exception? x, Exception? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        if (x.GetType() != y.GetType()) return false;

        // Handle inner exception checks
        if (x.InnerException is not null && y.InnerException is not null)
        {
            if (!this.Equals(x.InnerException, y.InnerException)) return false;
        }

        // One is not null
        if (x.InnerException is not null || y.InnerException is not null) return false;

        // Compare traces
        return x.StackTrace == y.StackTrace;
    }

    public int GetHashCode([DisallowNull] Exception obj) => HashCode.Combine(obj.GetType(), obj.Message, obj.StackTrace);
}
