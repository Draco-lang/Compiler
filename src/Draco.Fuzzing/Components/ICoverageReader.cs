using Draco.Coverage;

namespace Draco.Fuzzing.Components;

/// <summary>
/// Handles coverage information for a target.
/// </summary>
public interface ICoverageReader
{
    /// <summary>
    /// Clears the coverage information for the target.
    /// </summary>
    /// <param name="targetInfo">The target to clear coverage information for.</param>
    public void Clear(TargetInfo targetInfo);

    /// <summary>
    /// Reads the coverage information for the target.
    /// </summary>
    /// <param name="targetInfo">The target to read coverage information for.</param>
    /// <returns>The coverage information for the target.</returns>
    public CoverageResult Read(TargetInfo targetInfo);
}

/// <summary>
/// Factory for common coverage reading logic.
/// </summary>
public static class CoverageReader
{
    /// <summary>
    /// A default coverage reader that clears and reads directly from the supported target information.
    /// </summary>
    public static ICoverageReader Default => DefaultReader.Instance;

    private sealed class DefaultReader : ICoverageReader
    {
        public static DefaultReader Instance { get; } = new();

        private DefaultReader()
        {
        }

        public void Clear(TargetInfo targetInfo) => targetInfo.ClearCoverageData();
        public CoverageResult Read(TargetInfo targetInfo) => targetInfo.CoverageResult;
    }
}
