using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Chr.Constraints;
using Draco.Chr.Rules;
using Draco.Chr.Tracing;

namespace Draco.Chr.Solve;

/// <summary>
/// A base class for solvers that only need to implement rule prioritization.
/// </summary>
public abstract class Solver : ISolver
{
    private readonly record struct RuleAndMatch(Rule Rule, ImmutableArray<IConstraint> Match);

    /// <summary>
    /// The rules in priority order.
    /// </summary>
    public abstract IEnumerable<Rule> Rules { get; }

    /// <summary>
    /// The tracer of this solver.
    /// </summary>
    public ITracer Tracer { get; set; }

    public Solver(ITracer? tracer = null)
    {
        this.Tracer = tracer ?? NullTracer.Instance;
    }

    public ConstraintStore Solve(ConstraintStore store)
    {
        var history = new PropagationHistory();
        var iteratorStack = new Stack<IEnumerator<IConstraint>>();

        this.Tracer.Start(store);

        while (true)
        {
            var maybeRuleAndMatch = this.FindRuleAndMatch(store, history, iteratorStack);
            if (maybeRuleAndMatch is null) break;

            var ruleAndMatch = maybeRuleAndMatch.Value;
            // Remove head
            store.RemoveRange(ruleAndMatch.Match);

            if (ruleAndMatch.Rule.SaveHistory)
            {
                history.AddEntry(ruleAndMatch.Rule, ruleAndMatch.Match);
            }

            var newConstraints = ruleAndMatch.Rule.Apply(ruleAndMatch.Match);
            // Add back what's been produced
            store.AddRange(newConstraints);

            // Just for tracing
            this.Tracer.Step(ruleAndMatch.Rule, ruleAndMatch.Match, newConstraints.Except(ruleAndMatch.Match));
        }

        this.Tracer.End(store);
        return store;
    }

    private RuleAndMatch? FindRuleAndMatch(
        ConstraintStore store,
        PropagationHistory history,
        Stack<IEnumerator<IConstraint>> iteratorStack)
    {

    }
}
