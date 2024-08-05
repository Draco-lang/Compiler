using System.Collections.Generic;
using Draco.Chr.Constraints;
using Draco.Chr.Rules;

namespace Draco.Chr.Tracing;

/// <summary>
/// A tracer that can observe solver behavior.
/// </summary>
public interface ITracer
{
    /// <summary>
    /// Called before a rule is matched.
    /// </summary>
    /// <param name="rule">The matched rule.</param>
    /// <param name="constraints">The matched constraints.</param>
    /// <param name="store">The constraint store.</param>
    public void BeforeMatch(Rule rule, IEnumerable<IConstraint> constraints, ConstraintStore store);

    /// <summary>
    /// Called after a rule is matched.
    /// </summary>
    /// <param name="rule">The rule that was applied.</param>
    /// <param name="matchedConstraints">The constraints that were matched.</param>
    /// <param name="newConstraints">The constraints that were created.</param>
    /// <param name="store">The store after the rule applied.</param>
    public void AfterMatch(
        Rule rule,
        IEnumerable<IConstraint> matchedConstraints,
        IEnumerable<IConstraint> newConstraints,
        ConstraintStore store);

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

    /// <summary>
    /// Flushes any buffered output.
    /// </summary>
    public void Flush();
}
