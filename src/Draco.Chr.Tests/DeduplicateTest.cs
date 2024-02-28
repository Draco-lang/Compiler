using System.Collections.Immutable;
using Draco.Chr.Constraints;
using Draco.Chr.Rules;
using Draco.Chr.Solve;
using Xunit;

namespace Draco.Chr.Tests;

public sealed class DeduplicateTest
{
    [Fact]
    public void Deduplicate()
    {
        var numbers = new[] { 1, 2, 3, 4, 3, 1, 2, 3, 1, 3 };
        var expected = numbers.ToHashSet();

        var solver = new DefinitionOrderSolver(ConstructRules());
        var store = new ConstraintStore();
        store.AddRange(numbers);

        solver.Solve(store);

        Assert.Equal(expected.Count, store.Count);
        Assert.True(expected.SetEquals(store.Select(c => c.Value).OfType<int>()));
    }

    private static IEnumerable<Rule> ConstructRules()
    {
        var X = new Var("X");

        yield return new Simpagation(1, ImmutableArray.Create(
            Head.Any().Bind(X),
            Head.Any().Bind(X)));
    }
}
