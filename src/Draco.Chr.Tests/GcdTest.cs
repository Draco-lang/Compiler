using Draco.Chr.Constraints;
using Draco.Chr.Rules;
using Draco.Chr.Solve;
using Xunit;
using static Draco.Chr.Rules.RuleFactory;

namespace Draco.Chr.Tests;

public sealed class GcdTest
{
    [InlineData(3, 5, 1)]
    [InlineData(6, 9, 3)]
    [InlineData(9, 6, 3)]
    [InlineData(12, 15, 3)]
    [InlineData(15, 12, 3)]
    [InlineData(15, 15, 15)]
    [Theory]
    public void GreatestCommonDivisor(int n, int m, int expected)
    {
        var solver = new DefinitionOrderSolver(ConstructRules());
        var store = new ConstraintStore() { n, m };
        solver.Solve(store);

        var got = store
            .Select(c => c.Value)
            .OfType<int>()
            .Single();

        Assert.Equal(expected, got);
    }

    [InlineData(3, 5, 1, 1)]
    [InlineData(6, 9, 3, 3)]
    [InlineData(9, 6, 3, 3)]
    [InlineData(12, 15, 3, 3)]
    [InlineData(15, 12, 3, 3)]
    [InlineData(15, 15, 15, 15)]
    [InlineData(3, 7, 11, 1)]
    [Theory]
    public void GreatestCommonDivisorThreeWay(int n, int m, int o, int expected)
    {
        var solver = new DefinitionOrderSolver(ConstructRules());
        var store = new ConstraintStore() { n, m, o };
        solver.Solve(store);

        var got = store
            .Select(c => c.Value)
            .OfType<int>()
            .Single();

        Assert.Equal(expected, got);
    }

    private static IEnumerable<Rule> ConstructRules()
    {
        yield return Simpagation(typeof(int), Sep, typeof(int))
            .Guard((int n, int m) => n > 0 && n <= m)
            .Body((ConstraintStore store, int n, int m) =>
            {
                store.Add(m - n);
            });

        yield return Simplification(args: [0]);
    }
}
