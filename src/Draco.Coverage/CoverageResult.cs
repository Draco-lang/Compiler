using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Draco.Coverage;

/// <summary>
/// The result of a coverage run.
/// </summary>
public readonly struct CoverageResult
{
    /// <summary>
    /// An empty coverage result.
    /// </summary>
    public static CoverageResult Empty { get; } = new([], []);

    /// <summary>
    /// The sequence points of the weaved assembly.
    /// </summary>
    public ImmutableArray<SequencePoint> SequencePoints { get; }

    /// <summary>
    /// The hit counts of each sequence point.
    /// </summary>
    public ImmutableArray<int> Hits { get; }

    public CoverageResult(ImmutableArray<SequencePoint> sequencePoints, ImmutableArray<int> hits)
    {
        if (sequencePoints.Length != hits.Length)
        {
            throw new ArgumentException("the sequence points and hits arrays must have the same length");
        }

        this.SequencePoints = sequencePoints;
        this.Hits = hits;
    }

    /// <summary>
    /// Writes the coverage result in LCOV format.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    public void WriteLcov(Stream stream)
    {
        using var writer = new StreamWriter(stream);

        // We need to emit the data by file
        var dataByFile = this.SequencePoints
            .Zip(this.Hits, (sp, hits) => (SequencePoint: sp, Hits: hits))
            .GroupBy(x => x.SequencePoint.FileName);
        foreach (var group in dataByFile) WriteSequencePointsForFile(group.Key, group);

        void WriteSequencePointsForFile(string fileName, IEnumerable<(SequencePoint SequencePoint, int Hits)> data)
        {
            writer.WriteLine($"SF:{fileName}");

            // For each group, sequence points are grouped and sorted by start line
            var dataByLine = data
                .GroupBy(x => x.SequencePoint.StartLine)
                .OrderBy(x => x.Key);

            // Keep track of how many entries we have and how many are hit
            var entries = 0;
            var entriesHit = 0;

            foreach (var lineData in dataByLine)
            {
                var line = lineData.Key;
                // NOTE: To simplify things, if there are multiple sequence points on the same line,
                // we sum their hits together
                var hits = lineData.Sum(x => x.Hits);
                writer.WriteLine($"DA:{line},{hits}");
                ++entries;
                if (hits != 0) ++entriesHit;
            }

            // Write entry count
            writer.WriteLine($"LF:{entries}");
            writer.WriteLine($"LH:{entriesHit}");

            // End of file
            writer.WriteLine("end_of_record");
        }
    }
}
