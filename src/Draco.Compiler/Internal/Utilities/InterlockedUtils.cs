using System;
using System.Collections.Immutable;
using System.Threading;

namespace Draco.Compiler.Internal.Utilities;

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

    public static T? InitializeMaybeNull<T>(ref T? field, Func<T?> factory)
        where T : class
    {
        if (field is null) Interlocked.CompareExchange(ref field, factory(), null);
        return field;
    }

    public static ImmutableArray<T> InitializeDefault<T>(ref ImmutableArray<T> field, Func<ImmutableArray<T>> factory)
    {
        if (field.IsDefault) ImmutableInterlocked.InterlockedInitialize(ref field, factory());
        return field;
    }
}
