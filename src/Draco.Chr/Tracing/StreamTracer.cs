using System;
using System.Collections.Immutable;
using System.IO;
using Draco.Chr.Constraints;
using Draco.Chr.Rules;

namespace Draco.Chr.Tracing;

/// <summary>
/// A tracer that logs to a stream reader/writer.
/// </summary>
public sealed class StreamTracer : ITracer
{
    /// <summary>
    /// A tracer instance that uses the standard input/output streams.
    /// </summary>
    public static StreamTracer Stdio { get; } = new(
        new StreamReader(Console.OpenStandardInput()),
        new StreamWriter(Console.OpenStandardOutput()));

    /// <summary>
    /// True, if the tracer should wait for a keypress after each step.
    /// </summary>
    public bool WaitForKeypress { get; set; }

    private readonly StreamReader reader;
    private readonly StreamWriter writer;

    public StreamTracer(StreamReader reader, StreamWriter writer)
    {
        this.reader = reader;
        this.writer = writer;
    }

    public void Step(
        Rule appliedRule,
        ImmutableArray<IConstraint> matchedConstraints,
        ImmutableArray<IConstraint> newConstraints)
    {
        this.writer.WriteLine($"applied '{appliedRule.Name}'");
        this.writer.WriteLine($"  matched: {string.Join(", ", matchedConstraints)}");
        this.writer.WriteLine($"    added: {string.Join(", ", newConstraints)}");

        if (this.WaitForKeypress) this.reader.ReadLine();
    }

    public void Start(ConstraintStore store) => this.writer.WriteLine($"initial store: {string.Join(", ", store)}");
    public void End(ConstraintStore store) => this.writer.WriteLine($"final store: {string.Join(", ", store)}");
}
