using System;
using System.Collections.Generic;
using System.Linq;
using Draco.Chr.Rules;

namespace Draco.Chr.Constraints;

/// <summary>
/// Stores rule propagation history.
/// </summary>
internal sealed class PropagationHistory
{
    private readonly record struct Entry(IConstraint[] Constraints);

    private readonly Dictionary<Rule, List<Entry>> history = [];

    /// <summary>
    /// Adds a new entry to the history.
    /// </summary>
    /// <param name="rule">The rule to add an entry for.</param>
    /// <param name="constraints">The constraints that were propagated.</param>
    public void AddEntry(Rule rule, IEnumerable<IConstraint> constraints)
    {
        if (!this.history.TryGetValue(rule, out var entries))
        {
            entries = [];
            this.history.Add(rule, entries);
        }

        entries.Add(new(constraints.ToArray()));
    }

    /// <summary>
    /// Checks, if a rule with a given set of constraints is in the history.
    /// </summary>
    /// <param name="rule">The rule to check for.</param>
    /// <param name="constraints">The constraints to check for.</param>
    /// <returns>True, if the rule with the given constraints is in the history, false otherwise.</returns>
    public bool IsInHistory(Rule rule, IEnumerable<IConstraint> constraints)
    {
        if (!this.history.TryGetValue(rule, out var entries)) return false;

        return entries.Any(entry => entry.Constraints.SequenceEqual(constraints));
    }
}
