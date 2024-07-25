using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    /// <summary>
    /// True, if the store should only be printed at the end.
    /// </summary>
    public bool OnlyPrintStoreAtEnd { get; set; }

    public void BeforeMatch(Rule rule, IEnumerable<IConstraint> constraints, ConstraintStore store)
    {
        writer.WriteLine($"Applied {rule.Name}:");

        if (constraints.Any()) writer.WriteLine($" - Matched:");
        foreach (var m in constraints) writer.WriteLine($"   * {m}");
    }

    public void AfterMatch(
        Rule rule,
        IEnumerable<IConstraint> matchedConstraints,
        IEnumerable<IConstraint> newConstraints,
        ConstraintStore store)
    {
        if (newConstraints.Any()) writer.WriteLine($" - Added:");
        foreach (var a in newConstraints) writer.WriteLine($"   * {a}");

        if (!this.OnlyPrintStoreAtEnd) this.ListStore(store);

        if (this.WaitForKeypress) reader.ReadLine();
    }

    public void Start(ConstraintStore store) => this.ListStore(store);
    public void End(ConstraintStore store)
    {
        if (this.OnlyPrintStoreAtEnd) this.ListStore(store);
    }
    public void Flush() => writer.Flush();

    private void ListStore(ConstraintStore store)
    {
        if (store.Count == 0)
        {
            writer.WriteLine("Store: empty");
            return;
        }
        writer.WriteLine("Store:");
        foreach (var item in store) writer.WriteLine($" - {item}");
    }
}
