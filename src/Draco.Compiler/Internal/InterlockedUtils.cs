using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal;

/// <summary>
/// Utility functions for atomic operations.
/// </summary>
internal static class InterlockedUtils
{
    public static T InitializeNull<T>(ref T? field, Func<T> factory)
        where T : class
    {
        if (field is null) Interlocked.CompareExchange(ref field, factory(), null);
        return field;
    }

    public static ImmutableArray<T> InizializeDefault<T>(ref ImmutableArray<T> field, Func<ImmutableArray<T>> factory)
    {
        if (field.IsDefault) ImmutableInterlocked.InterlockedInitialize(ref field, factory());
        return field;
    }
}
