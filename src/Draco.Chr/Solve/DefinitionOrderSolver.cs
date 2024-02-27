using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Chr.Rules;

namespace Draco.Chr.Solve;

/// <summary>
/// A solver that prioritizes rules based on their definition order.
/// </summary>
public sealed class DefinitionOrderSolver : PrioritizingSolver
{
    public override IEnumerable<Rule> Rules { get; }

    public DefinitionOrderSolver(IEnumerable<Rule> rules)
    {
        this.Rules = rules.ToList();
    }
}
