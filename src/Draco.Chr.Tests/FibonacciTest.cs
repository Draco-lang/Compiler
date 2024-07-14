using System.Collections.Immutable;
using Draco.Chr.Constraints;
using Draco.Chr.Rules;
using Draco.Chr.Solve;
using Xunit;
using static Draco.Chr.Rules.RuleFactory;

namespace Draco.Chr.Tests;

public sealed class FibonacciTest
{
    private readonly record struct Fib(int Index, int Value)
    {
        public override string ToString() => $"fib[{this.Index}] = {this.Value}";
    }

    [Fact]
    public void Compute()
    {
        const int amount = 42;

        var solver = new DefinitionOrderSolver(ConstructRules());
        var store = new ConstraintStore()
        {
            amount - 1,
        };
        solver.Solve(store);

        var expected = Oracle()
            .Take(amount)
            .ToArray();
        var got = store
            .Select(c => c.Value)
            .OfType<Fib>()
            .OrderBy(fib => fib.Index)
            .Select(f => f.Value)
            .ToArray();

        Assert.True(expected.SequenceEqual(got));
    }

    private static IEnumerable<int> Oracle()
    {
        var a = 1;
        var b = 0;
        while (true)
        {
            yield return b;
            var c = a + b;
            a = b;
            b = c;
        }
    }

    private static IEnumerable<Rule> ConstructRules()
    {
        yield return Propagation(typeof(int))
            .Body((ConstraintStore store, int _) =>
            {
                store.Add(new Fib(0, 0));
                store.Add(new Fib(1, 1));
            });

        yield return Propagation(typeof(int), typeof(Fib), typeof(Fib))
            .Guard((int idx, Fib f1, Fib f2) => f1.Index == f2.Index - 1
                                             && f2.Index < idx)
            .Body((ConstraintStore store, int _, Fib f1, Fib f2) =>
                store.Add(new Fib(f2.Index + 1, f1.Value + f2.Value)));

        yield return Simpagation(typeof(Fib), Sep, typeof(int))
            .Guard((Fib fib, int idx) => idx == fib.Index);
    }
}
