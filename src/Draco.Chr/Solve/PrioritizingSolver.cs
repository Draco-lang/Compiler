using System;
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
public abstract class PrioritizingSolver : ISolver
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

    public PrioritizingSolver(ITracer? tracer = null)
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
        foreach (var rule in this.Rules)
        {
            var matchingConstraints = FindMatchingHeadsForRule(rule, store, iteratorStack, history);
            if (matchingConstraints is not null) return new RuleAndMatch(rule, matchingConstraints.Value);
        }
        return null;
    }

    private static ImmutableArray<IConstraint>? FindMatchingHeadsForRule(
        Rule rule,
        ConstraintStore store,
        Stack<IEnumerator<IConstraint>> iteratorStack,
        PropagationHistory history)
    {
        if (store.Count < rule.HeadCount) return null;

        var pointer = 0;
        var matchingHeads = ImmutableArray.CreateBuilder<IConstraint>(rule.HeadCount);


    }

    private static IEnumerator<IConstraint> GetHeadEnumerator(Rule rule, ConstraintStore store, int pointer) => rule.DefinitionType switch
    {
        HeadListType.SizeSpecified => GetDefaultIterator(store),
        HeadListType.TypesSpecified => GetTypedHeadIterator(store, rule, pointer),
        HeadListType.ComplexDefinition => GetComplexHeadIterator(store, rule, pointer),
        _ => throw new InvalidOperationException(),
    };

    private static IEnumerator<IConstraint> GetDefaultIterator(ConstraintStore store) =>
        store.GetEnumerator();
    private static IEnumerator<IConstraint> GetTypedHeadIterator(ConstraintStore store, Rule rule, int pointer) =>
        store.ConstraintsOfType(rule.HeadTypes[pointer]).GetEnumerator();
    private static IEnumerator<IConstraint> GetComplexHeadIterator(ConstraintStore store, Rule rule, int pointer)
    {
        var head = rule.HeadDefinitions[pointer];
        return head.HeadContains switch
        {
            HeadContains.Any => GetDefaultIterator(store),
            HeadContains.Type => store.ConstraintsOfType(head.Type!).GetEnumerator(),
            HeadContains.Value => store.ConstraintsWithValue(head.Value!).GetEnumerator(),
            _ => throw new InvalidOperationException(),
        };
    }
}
