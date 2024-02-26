using Draco.Chr.Constraints;
using System.Collections.Immutable;
using Draco.Chr.Rules;

namespace Draco.Chr.Tracing;

/// <summary>
/// A tracer that can observe solver behavior.
/// </summary>
public interface ITracer
{
    /// <summary>
    /// Called when a rule is applied.
    /// </summary>
    /// <param name="appliedRule">The rule that was applied.</param>
    /// <param name="matchedConstraints">The constraints that were matched.</param>
    /// <param name="newConstraints">The constraints that were created.</param>
    public void Step(
        Rule appliedRule,
        ImmutableArray<IConstraint> matchedConstraints,
        ImmutableArray<IConstraint> newConstraints);

    /// <summary>
    /// Called when the solver is started.
    /// </summary>
    /// <param name="store">The initial constraint store.</param>
    public void Start(ConstraintStore store);

    /// <summary>
    /// Called when the solver has finished.
    /// </summary>
    /// <param name="store">The final constraint store.</param>
    public void End(ConstraintStore store);
}
