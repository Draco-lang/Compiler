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

    public void Start(ConstraintStore store)
    {
        foreach (var tracer in this.tracers) tracer.Start(store);
    }

    public void End(ConstraintStore store)
    {
        foreach (var tracer in this.tracers) tracer.End(store);
    }

    public void Step(Rule appliedRule, IEnumerable<IConstraint> matchedConstraints, IEnumerable<IConstraint> newConstraints)
    {
        foreach (var tracer in this.tracers) tracer.Step(appliedRule, matchedConstraints, newConstraints);
    }

    public void Flush()
    {
        foreach (var tracer in this.tracers) tracer.Flush();
    }
}
