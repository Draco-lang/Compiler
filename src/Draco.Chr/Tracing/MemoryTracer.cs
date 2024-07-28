using System.Collections.Generic;
using System.IO;
using Draco.Chr.Constraints;
using Draco.Chr.Rules;

namespace Draco.Chr.Tracing;

/// <summary>
/// A tracer logging into memory.
/// </summary>
public sealed class MemoryTracer : ITracer
{
    /// <summary>
    /// The output stream.
    /// </summary>
    public MemoryStream Output { get; } = new();

    private readonly StreamTracer underlyingTracer;

    public MemoryTracer()
    {
        this.underlyingTracer = new(StreamReader.Null, new StreamWriter(this.Output));
    }

    public void Start(ConstraintStore store) => this.underlyingTracer.Start(store);
    public void End(ConstraintStore store) => this.underlyingTracer.End(store);
    public void AfterMatch(
        Rule rule,
        IEnumerable<IConstraint> matchedConstraints,
        IEnumerable<IConstraint> newConstraints, ConstraintStore store) =>
        this.underlyingTracer.AfterMatch(rule, matchedConstraints, newConstraints, store);
    public void BeforeMatch(
        Rule rule,
        IEnumerable<IConstraint> constraints,
        ConstraintStore store) => this.underlyingTracer.BeforeMatch(rule, constraints, store);
    public void Flush() => this.underlyingTracer.Flush();

    public override string ToString()
    {
        var oldPosition = this.Output.Position;
        this.Output.Position = 0;
        var reader = new StreamReader(this.Output);
        var result = reader.ReadToEnd();
        this.Output.Position = oldPosition;
        return result;
    }
}
