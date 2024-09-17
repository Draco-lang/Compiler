using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Draco.Coverage;

/// <summary>
/// A template for a coverage collector that gets weaved into the target assembly.
/// </summary>
internal static class CoverageCollectorTemplate
{
    public readonly struct SequencePoint
    {
        public readonly string FileName;
        public readonly int Offset;
        public readonly int StartLine;
        public readonly int StartColumn;
        public readonly int EndLine;
        public readonly int EndColumn;

        public SequencePoint(string fileName, int offset, int startLine, int startColumn, int endLine, int endColumn)
        {
            this.FileName = fileName;
            this.Offset = offset;
            this.StartLine = startLine;
            this.StartColumn = startColumn;
            this.EndLine = endLine;
            this.EndColumn = endColumn;
        }
    }

    public static readonly int[] Hits = null!;
    public static readonly ImmutableArray<SequencePoint> SequencePoints;

    static CoverageCollectorTemplate()
    {
        // The weaver will need to instantiate hits and fill in the sequence points array
    }

    public static void RecordHit(string fileName, int index) => Interlocked.Increment(ref Hits[index]);

    public static void Clear() => Array.Clear(Hits, 0, Hits.Length);
}
