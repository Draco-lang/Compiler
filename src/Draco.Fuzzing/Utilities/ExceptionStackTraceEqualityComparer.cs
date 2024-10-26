using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Draco.Fuzzing.Utilities;

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

        // If both traces are null, all we have is the messages to compare
        if (x.StackTrace is null && y.StackTrace is null) return x.Message == y.Message;

        // NOTE: If we have traces, we don't compare messages, in case there is some specific
        // name or identifier that would be an insignificant difference

        // Compare traces
        return x.StackTrace == y.StackTrace;
    }

    public int GetHashCode([DisallowNull] Exception obj) => HashCode.Combine(obj.GetType(), obj.Message, obj.StackTrace);
}
