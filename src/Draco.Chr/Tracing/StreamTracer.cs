using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Draco.Chr.Constraints;
using Draco.Chr.Rules;

namespace Draco.Chr.Tracing;

/// <summary>
/// A tracer that logs to a stream reader/writer.
/// </summary>
public sealed class StreamTracer(StreamReader reader, StreamWriter writer) : ITracer
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

    public void Step(
        Rule appliedRule,
        IEnumerable<IConstraint> matchedConstraints,
        IEnumerable<IConstraint> newConstraints)
    {
        writer.WriteLine($"applied '{appliedRule.Name}'");
        writer.WriteLine($"  matched: {string.Join(", ", matchedConstraints)}");
        writer.WriteLine($"    added: {string.Join(", ", newConstraints)}");

        if (this.WaitForKeypress) reader.ReadLine();
    }

    public void Start(ConstraintStore store) => writer.WriteLine($"initial store: {string.Join(", ", store)}");
    public void End(ConstraintStore store) => writer.WriteLine($"final store: {string.Join(", ", store)}");
}
