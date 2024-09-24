using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Draco.Fuzzing;

/// <summary>
/// Checks for the equality of two <see cref="FaultResult"/> instances for the purpose of treating them equivalent.
/// </summary>
internal sealed class FaultEqualityComparer : IEqualityComparer<FaultResult>
{
    /// <summary>
    /// A singleton instance of the comparer.
    /// </summary>
    public static FaultEqualityComparer Instance { get; } = new();

    private FaultEqualityComparer()
    {
    }

    public bool Equals(FaultResult x, FaultResult y)
    {
        if (x.IsFaulted != y.IsFaulted) return false;
        if (x.ThrownException?.GetType() != y.ThrownException?.GetType()) return false;
        if (x.TimeoutReached != y.TimeoutReached) return false;
        // NOTE: We don't compare the error message, because it might be different for the same error
        // NOTE: We don't compare the exit code, because it might be the same for different crashes
        return true;
    }

    public int GetHashCode([DisallowNull] FaultResult obj) =>
        HashCode.Combine(obj.ThrownException?.GetType(), obj.TimeoutReached);
}
