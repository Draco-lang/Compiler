using System.Collections.Generic;
using Draco.Chr.Constraints;
using Draco.Chr.Rules;

namespace Draco.Chr.Tracing;

/// <summary>
/// A tracer that discards all information.
/// </summary>
public sealed class NullTracer : ITracer
{
    /// <summary>
    /// A singleton instance of the tracer.
    /// </summary>
    public static NullTracer Instance { get; } = new();

    private NullTracer()
    {
    }

    public void Step(
        Rule appliedRule,
        IEnumerable<IConstraint> matchedConstraints,
        IEnumerable<IConstraint> newConstraints,
        ConstraintStore store)
    {
    }

    public void Start(ConstraintStore store) { }
    public void End(ConstraintStore store) { }
    public void Flush() { }
}
