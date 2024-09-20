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
    /// A hash-based coverage compressor.
    /// </summary>
    public static ICoverageCompressor<int> Hash => HashCoverageCompressor.Instance;

    private sealed class HashCoverageCompressor : ICoverageCompressor<int>
    {
        public static HashCoverageCompressor Instance { get; } = new();

        private HashCoverageCompressor()
        {
        }

        public int Compress(CoverageResult result)
        {
            // NOTE: Naive, slow implementation, we might need to come back to vectorize this
            // We create a bitarray of the 0 and nonzero hit positions then hash combine them
            var hash = default(HashCode);
            foreach (var entry in result.Entires)
            {
                hash.Add(entry.Hits != 0);
            }
            return hash.ToHashCode();
        }
    }
}
