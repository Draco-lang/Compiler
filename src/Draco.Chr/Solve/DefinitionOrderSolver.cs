using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Draco.Chr.Rules;
using Draco.Chr.Tracing;

namespace Draco.Chr.Solve;

/// <summary>
/// A solver that prioritizes rules based on their definition order.
/// </summary>
public sealed class DefinitionOrderSolver(
    IEnumerable<Rule> rules,
    IEqualityComparer? comparer = null,
    ITracer? tracer = null) : PrioritizingSolver(comparer, tracer)
{
    public override IEnumerable<Rule> Rules { get; } = rules.ToList();
}
