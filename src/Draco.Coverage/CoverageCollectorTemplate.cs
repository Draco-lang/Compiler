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
        public readonly int Offset;
        public readonly int StartLine;
        public readonly int StartColumn;
        public readonly int EndLine;
        public readonly int EndColumn;

        public SequencePoint(int offset, int startLine, int startColumn, int endLine, int endColumn)
        {
            this.Offset = offset;
            this.StartLine = startLine;
            this.StartColumn = startColumn;
            this.EndLine = endLine;
            this.EndColumn = endColumn;
        }
    }

    public sealed class FileCoverage
    {
        public readonly string FileName;
        public readonly int[] Hits;
        public readonly ImmutableArray<SequencePoint> SequencePoints;

        public FileCoverage(string fileName, ImmutableArray<SequencePoint> sequencePoints)
        {
            this.FileName = fileName;
            this.Hits = new int[sequencePoints.Length];
            this.SequencePoints = sequencePoints;
        }

        public void RecordHit(int index) => Interlocked.Increment(ref this.Hits[index]);
    }

    public static readonly Dictionary<string, FileCoverage> FileCoverages;

    static CoverageCollectorTemplate()
    {
        FileCoverages = new();

        // The weaver will need to add a coverage entry for each file here with all of the sequence points
    }

    public static void RecordHit(string fileName, int index)
    {
        if (!FileCoverages.TryGetValue(fileName, out var fileCoverage)) return;
        fileCoverage.RecordHit(index);
    }
}
