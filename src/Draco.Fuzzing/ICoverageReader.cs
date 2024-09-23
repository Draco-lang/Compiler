using System;
using Draco.Coverage;

namespace Draco.Fuzzing;

/// <summary>
/// Reads the coverage data from the target.
/// </summary>
public interface ICoverageReader
{
    /// <summary>
    /// Reads the coverage data from the target.
    /// </summary>
    /// <returns>The coverage data.</returns>
    public CoverageResult Read();

    /// <summary>
    /// Clears the coverage data.
    /// </summary>
    public void Clear();
}

/// <summary>
/// Factory for common coverage reading logic.
/// </summary>
public static class CoverageReader
{
    /// <summary>
    /// Creates a coverage reader from the given functions.
    /// </summary>
    /// <param name="read">The function to read the coverage.</param>
    /// <param name="clear">The function to clear the coverage.</param>
    /// <returns>The coverage reader.</returns>
    public static ICoverageReader Create(Func<CoverageResult> read, Action clear) => new DelegateReader(read, clear);

    /// <summary>
    /// Creates a coverage reader that reads from the given instrumented assembly.
    /// </summary>
    /// <param name="assembly">The instrumented assembly to read from.</param>
    /// <returns>The coverage reader.</returns>
    public static ICoverageReader FromInstrumentedAssembly(InstrumentedAssembly assembly) =>
        Create(() => assembly.CoverageResult, assembly.ClearCoverageData);

    /// <summary>
    /// Creates a coverage reader that reads from the given processes shared memory.
    /// </summary>
    /// <param name="processReference">The process reference.</param>
    /// <returns>The coverage reader.</returns>
    public static ICoverageReader FromProcess(ProcessReference processReference) =>
        Create(() => CoverageResult.FromSharedMemory(processReference.SharedMemory), () => processReference.SharedMemory.Span.Clear());

    private sealed class DelegateReader(
        Func<CoverageResult> read,
        Action clear) : ICoverageReader
    {
        public CoverageResult Read() => read();
        public void Clear() => clear();
    }
}
