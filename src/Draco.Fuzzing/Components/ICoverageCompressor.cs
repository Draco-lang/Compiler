using System;
using System.Runtime.InteropServices;
using Draco.Coverage;
using Draco.Fuzzing.Utilities;

namespace Draco.Fuzzing.Components;

/// <summary>
/// Compresses the coverage result into a type that can be stored in an associative collection.
/// </summary>
/// <typeparam name="TCoverage">The type of the compressed coverage data.</typeparam>
public interface ICoverageCompressor<TCoverage>
{
    /// <summary>
    /// Compresses the given coverage result.
    /// </summary>
    /// <param name="result">The coverage result to compress.</param>
    /// <returns>The compressed coverage data.</returns>
    public TCoverage Compress(CoverageResult result);
}

/// <summary>
/// Factory for common coverage compression logic.
/// </summary>
public static class CoverageCompressor
{
    /// <summary>
    /// Creates a coverage compressor from the given function.
    /// </summary>
    /// <typeparam name="TCoverage">The type of the compressed coverage data.</typeparam>
    /// <param name="func">The function to compress the coverage.</param>
    /// <returns>The coverage compressor.</returns>
    public static ICoverageCompressor<TCoverage> Create<TCoverage>(Func<CoverageResult, TCoverage> func) =>
        new DelegateCompressor<TCoverage>(func);

    /// <summary>
    /// A naive hash-based coverage compressor.
    /// </summary>
    public static ICoverageCompressor<int> NaiveHash { get; } = Create(result =>
    {
        var hash = default(HashCode);
        foreach (var h in result.Hits) hash.Add(h != 0);
        return hash.ToHashCode();
    });

    /// <summary>
    /// A SIMD-optimized hash-based coverage compressor.
    /// </summary>
    public static ICoverageCompressor<int> SimdHash { get; } = Create(result =>
    {
        var hits = GC.AllocateUninitializedArray<int>(result.Hits.Length);
        result.Hits.CopyTo(hits);
        SimdUtilities.InPlaceEqualityCompareToZero(hits);
        // Get a byte span from hits
        var span = MemoryMarshal.AsBytes<int>(hits);
        var hash = default(HashCode);
        hash.AddBytes(span);
        return hash.ToHashCode();
    });

    private sealed class DelegateCompressor<TCoverage>(Func<CoverageResult, TCoverage> func) : ICoverageCompressor<TCoverage>
    {
        public TCoverage Compress(CoverageResult result) => func(result);
    }
}
