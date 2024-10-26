using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Draco.Fuzzing.Utilities;

/// <summary>
/// SIMD utilities.
/// </summary>
internal static class SimdUtilities
{
    public static void InPlaceEqualityCompareToZero(Span<int> span)
    {
        if (Vector512.IsHardwareAccelerated) InPlaceEqualityCompareToZero512(span);
        else if (Vector256.IsHardwareAccelerated) InPlaceEqualityCompareToZero256(span);
        else if (Vector128.IsHardwareAccelerated) InPlaceEqualityCompareToZero128(span);
        else InPlaceEqualityCompareToZeroFallback(span);
    }

    private static unsafe void InPlaceEqualityCompareToZero128(Span<int> span)
    {
        if (span.Length < Vector128<int>.Count)
        {
            InPlaceEqualityCompareToZeroFallback(span);
            return;
        }
        ref var endM1Vector = ref Unsafe.SubtractByteOffset(
            ref Unsafe.Add(ref MemoryMarshal.GetReference(span), span.Length),
            sizeof(Vector128<int>));
        ref var current = ref MemoryMarshal.GetReference(span);
        while (Unsafe.IsAddressLessThan(in current, in endM1Vector))
        {
            Vector128.StoreUnsafe(~Vector128.Equals(Vector128.LoadUnsafe(in current), Vector128.Create(0)), ref current);
            current = ref Unsafe.AddByteOffset(ref current, sizeof(Vector128<int>));
        }
        Vector128.StoreUnsafe(~Vector128.Equals(Vector128.LoadUnsafe(in endM1Vector), Vector128.Create(0)), ref endM1Vector);
    }

    private static unsafe void InPlaceEqualityCompareToZero256(Span<int> span)
    {
        if (span.Length < Vector256<int>.Count)
        {
            InPlaceEqualityCompareToZeroFallback(span);
            return;
        }
        ref var endM1Vector = ref Unsafe.SubtractByteOffset(
            ref Unsafe.Add(ref MemoryMarshal.GetReference(span), span.Length),
            sizeof(Vector256<int>));
        ref var current = ref MemoryMarshal.GetReference(span);
        while (Unsafe.IsAddressLessThan(in current, in endM1Vector))
        {
            Vector256.StoreUnsafe(~Vector256.Equals(Vector256.LoadUnsafe(in current), Vector256.Create(0)), ref current);
            current = ref Unsafe.AddByteOffset(ref current, sizeof(Vector256<int>));
        }
        Vector256.StoreUnsafe(~Vector256.Equals(Vector256.LoadUnsafe(in endM1Vector), Vector256.Create(0)), ref endM1Vector);
    }

    private static unsafe void InPlaceEqualityCompareToZero512(Span<int> span)
    {
        if (span.Length < Vector512<int>.Count)
        {
            InPlaceEqualityCompareToZeroFallback(span);
            return;
        }
        ref var endM1Vector = ref Unsafe.SubtractByteOffset(
            ref Unsafe.Add(ref MemoryMarshal.GetReference(span), span.Length),
            sizeof(Vector512<int>));
        ref var current = ref MemoryMarshal.GetReference(span);
        while (Unsafe.IsAddressLessThan(in current, in endM1Vector))
        {
            Vector512.StoreUnsafe(~Vector512.Equals(Vector512.LoadUnsafe(in current), Vector512.Create(0)), ref current);
            current = ref Unsafe.AddByteOffset(ref current, sizeof(Vector512<int>));
        }
        Vector512.StoreUnsafe(~Vector512.Equals(Vector512.LoadUnsafe(in endM1Vector), Vector512.Create(0)), ref endM1Vector);
    }

    private static void InPlaceEqualityCompareToZeroFallback(Span<int> span)
    {
        foreach (ref var value in span) value = value == 0 ? 0 : 0xFF;
    }
}
