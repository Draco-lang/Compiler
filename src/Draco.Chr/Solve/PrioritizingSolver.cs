using System;
using System.Collections;
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
public abstract class PrioritizingSolver(
    IEqualityComparer? comparer = null,
    ITracer? tracer = null) : ISolver
{
    private readonly record struct RuleAndMatch(Rule Rule, ImmutableArray<IConstraint> Match);

    /// <summary>
    /// The rules in priority order.
    /// </summary>
    public abstract IEnumerable<Rule> Rules { get; }

    /// <summary>
    /// The tracer of this solver.
    /// </summary>
    public ITracer Tracer { get; set; } = tracer ?? NullTracer.Instance;

    /// <summary>
    /// The comparer used for values.
    /// </summary>
    public IEqualityComparer ValueComparer { get; } = comparer ?? EqualityComparer<object>.Default;

    public PrioritizingSolver(ITracer? tracer)
        : this(null, tracer)
    {
    }

    public ConstraintStore Solve(ConstraintStore store)
    {
        var history = new PropagationHistory();
        var enumeratorStack = new Stack<IEnumerator<IConstraint>>();

        this.Tracer.Start(store);

        while (true)
        {
            var maybeRuleAndMatch = this.FindRuleAndMatch(store, history, enumeratorStack);
            if (maybeRuleAndMatch is null) break;

            var ruleAndMatch = maybeRuleAndMatch.Value;

            // Just for tracing
            this.Tracer.BeforeMatch(ruleAndMatch.Rule, ruleAndMatch.Match, store);

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
            this.Tracer.AfterMatch(ruleAndMatch.Rule, ruleAndMatch.Match, newConstraints.Except(ruleAndMatch.Match), store);
        }

        this.Tracer.End(store);
        this.Tracer.Flush();
        return store;
    }

    private RuleAndMatch? FindRuleAndMatch(
        ConstraintStore store,
        PropagationHistory history,
        Stack<IEnumerator<IConstraint>> enumeratorStack)
    {
        foreach (var rule in this.Rules)
        {
            var matchingConstraints = this.FindMatchingHeadsForRule(rule, store, enumeratorStack, history);
            if (matchingConstraints is not null) return new RuleAndMatch(rule, matchingConstraints.Value);
        }
        return null;
    }

    private ImmutableArray<IConstraint>? FindMatchingHeadsForRule(
        Rule rule,
        ConstraintStore store,
        Stack<IEnumerator<IConstraint>> enumeratorStack,
        PropagationHistory history)
    {
        if (store.Count < rule.HeadCount) return null;

        var pointer = 0;
        var matchingHeads = ImmutableArray.CreateBuilder<IConstraint>(rule.HeadCount);
        matchingHeads.Count = rule.HeadCount;
        var currentEnum = this.GetHeadEnumerator(rule, store, pointer);

        while (true)
        {
            var hasNext = currentEnum.MoveNext();
            if (pointer < rule.HeadCount - 1 && hasNext)
            {
                matchingHeads[pointer] = currentEnum.Current;
                enumeratorStack.Push(currentEnum);

                ++pointer;
                currentEnum = this.GetHeadEnumerator(rule, store, pointer);
            }
            else if (hasNext)
            {
                matchingHeads[pointer] = currentEnum.Current;

                if (AllUnique(matchingHeads)
                 && (rule.DefinitionType != HeadListType.ComplexDefinition || CheckBindings(rule.VariableBindings, matchingHeads))
                 && (!rule.SaveHistory || !history.IsInHistory(rule, matchingHeads))
                 && rule.Accepts(matchingHeads))
                {
                    return matchingHeads.ToImmutable();
                }
            }
            else if (pointer > 0)
            {
                --pointer;
                currentEnum = enumeratorStack.Pop();
            }
            else
            {
                break;
            }
        }

        return null;
    }

    private IEnumerator<IConstraint> GetHeadEnumerator(Rule rule, ConstraintStore store, int pointer) => rule.DefinitionType switch
    {
        HeadListType.SizeSpecified => GetDefaultEnumerator(store),
        HeadListType.TypesSpecified => GetTypedHeadEnumerator(store, rule, pointer),
        HeadListType.ComplexDefinition => this.GetComplexHeadEnumerator(store, rule, pointer),
        _ => throw new InvalidOperationException(),
    };

    private static IEnumerator<IConstraint> GetDefaultEnumerator(ConstraintStore store) =>
        store.GetEnumerator();
    private static IEnumerator<IConstraint> GetTypedHeadEnumerator(ConstraintStore store, Rule rule, int pointer) =>
        store.ConstraintsOfType(rule.HeadTypes[pointer]).GetEnumerator();
    private IEnumerator<IConstraint> GetComplexHeadEnumerator(ConstraintStore store, Rule rule, int pointer)
    {
        var head = rule.HeadDefinitions[pointer];
        return head.HeadContains switch
        {
            HeadContains.Any => GetDefaultEnumerator(store),
            HeadContains.Type => store.ConstraintsOfType(head.Type!).GetEnumerator(),
            HeadContains.Value => store.ConstraintsOfValue(head.Value!, this.ValueComparer).GetEnumerator(),
            _ => throw new InvalidOperationException(),
        };
    }

    private static bool AllUnique(ImmutableArray<IConstraint>.Builder constraints) =>
        constraints.Count == constraints.Distinct().Count();

    private static bool CheckBindings(
        IReadOnlyDictionary<Var, ImmutableArray<int>> variableBindings,
        ImmutableArray<IConstraint>.Builder heads)
    {
        foreach (var bound in variableBindings.Values)
        {
            for (var i = 0; i < bound.Length - 1; ++i)
            {
                if (!Equals(heads[bound[i]].Value, heads[bound[i + 1]].Value)) return false;
            }
        }
        return true;
    }
}
