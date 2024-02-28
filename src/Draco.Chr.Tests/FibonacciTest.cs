using System.Collections.Immutable;
using Draco.Chr.Constraints;
using Draco.Chr.Rules;
using Draco.Chr.Solve;
using Xunit;

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
        yield return new Propagation("seed", ImmutableArray.Create(Head.OfType(typeof(int))))
            .WithBody((_, store) =>
            {
                store.Add(new Fib(0, 0));
                store.Add(new Fib(1, 1));
            });
        yield return new Propagation(
            "acc",
            ImmutableArray.Create(
                Head.OfType(typeof(int)),
                Head.OfType(typeof(Fib)),
                Head.OfType(typeof(Fib))))
            .WithGuard(head => ((Fib)head[1].Value).Index == ((Fib)head[2].Value).Index - 1
                            && ((Fib)head[2].Value).Index < ((int)head[0].Value))
            .WithBody((head, store) =>
            {
                var index = ((Fib)head[2].Value).Index;
                var a = ((Fib)head[1].Value).Value;
                var b = ((Fib)head[2].Value).Value;
                store.Add(new Fib(index + 1, a + b));
            });
        yield return new Simpagation("term", 1, ImmutableArray.Create(Head.OfType(typeof(Fib)), Head.OfType(typeof(int))))
            .WithGuard((keep, remove) => remove[0].Value.Equals(((Fib)keep[0].Value).Index));
    }
}
