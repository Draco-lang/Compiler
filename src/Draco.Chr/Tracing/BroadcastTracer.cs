using System.Collections.Generic;
using System.Linq;
using Draco.Chr.Constraints;
using Draco.Chr.Rules;

namespace Draco.Chr.Tracing;

/// <summary>
/// A tracer that broadcasts events to a collection of tracers.
/// </summary>
public sealed class BroadcastTracer(IEnumerable<ITracer> tracers) : ITracer
{
    private readonly List<ITracer> tracers = tracers.ToList();

    public BroadcastTracer(params ITracer[] tracers)
        : this(tracers.AsEnumerable())
    {
    }

    public void Start(ConstraintStore store)
    {
        foreach (var tracer in this.tracers) tracer.Start(store);
    }

    public void End(ConstraintStore store)
    {
        foreach (var tracer in this.tracers) tracer.End(store);
    }

    public void BeforeMatch(Rule rule, IEnumerable<IConstraint> constraints, ConstraintStore store)
    {
        foreach (var tracer in this.tracers) tracer.BeforeMatch(rule, constraints, store);
    }

    public void AfterMatch(
        Rule rule,
        IEnumerable<IConstraint> matchedConstraints,
        IEnumerable<IConstraint> newConstraints,
        ConstraintStore store)
    {
        foreach (var tracer in this.tracers) tracer.AfterMatch(rule, matchedConstraints, newConstraints, store);
    }

    public void Flush()
    {
        foreach (var tracer in this.tracers) tracer.Flush();
    }
}
