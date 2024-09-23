using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Draco.Coverage;

/// <summary>
/// A template for a coverage collector that gets weaved into the target assembly.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class CoverageCollector
{
    public static readonly SequencePoint[] SequencePoints = null!;
    public static readonly SharedMemory<int> Hits = null!;

    static CoverageCollector()
    {
        // The weaver will need to instantiate hits and fill in the sequence points array
    }

    public static void RecordHit(int index) => Interlocked.Increment(ref Hits.Span[index]);

    public static void Clear() => Hits.Span.Clear();

    private static SharedMemory<int> AllocateHits(int length)
    {
        var sharedMemoryName = Environment.GetEnvironmentVariable(InstrumentedAssembly.SharedMemoryEnvironmentVariable);
        return sharedMemoryName is null
            ? SharedMemory.CreateNew<int>(InstrumentedAssembly.SharedMemoryKey(Guid.NewGuid().ToString()), length)
            : SharedMemory.OpenExisting<int>(InstrumentedAssembly.SharedMemoryKey(sharedMemoryName), length);
    }
}
