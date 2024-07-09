using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Chr.Rules;

namespace Draco.Chr.Solve;

/// <summary>
/// A solver that prioritizes rules based on their definition order.
/// </summary>
public sealed class DefinitionOrderSolver(IEnumerable<Rule> rules) : PrioritizingSolver
{
    public override IEnumerable<Rule> Rules { get; } = rules.ToList();
}
