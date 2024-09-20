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
