using System;
using Draco.Coverage;

namespace Draco.Fuzzing;

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
        // NOTE: Naive, slow implementation, we might need to come back to vectorize this
        // We create a bitarray of the 0 and nonzero hit positions then hash combine them
        var hash = default(HashCode);
        foreach (var h in result.Hits) hash.Add(h != 0);
        return hash.ToHashCode();
    });

    private sealed class DelegateCompressor<TCoverage>(Func<CoverageResult, TCoverage> func) : ICoverageCompressor<TCoverage>
    {
        public TCoverage Compress(CoverageResult result) => func(result);
    }
}
