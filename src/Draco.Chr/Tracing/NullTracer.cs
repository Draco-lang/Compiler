using System.Collections.Immutable;
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
        ImmutableArray<IConstraint> matchedConstraints,
        ImmutableArray<IConstraint> newConstraints)
    {
    }

    public void Start(ConstraintStore store) { }
    public void End(ConstraintStore store) { }
}
