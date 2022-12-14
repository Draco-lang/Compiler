using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Tests.EndToEnd;

public sealed class CompilingCodeTests : EndToEndTestsBase
{
    [Fact]
    public void Max()
    {
        var assembly = Compile("""
            func max(a: int32, b: int32): int32 = if (a > b) a else b;
            """);

        var inputs = new[] { (0, 0), (1, 0), (0, 1), (5, 0), (5, 4), (4, 5) };
        foreach (var (a, b) in inputs)
        {
            var maxi = Invoke<int>(assembly, "max", a, b);
            Assert.Equal(Math.Max(a, b), maxi);
        }
    }

    [Fact]
    public void Abs()
    {
        var assembly = Compile("""
            func abs(n: int32): int32 = if (n > 0) n else -n;
            """);

        var inputs = new[] { 0, 1, -1, 3, 8, -3, -5 };
        foreach (var n in inputs)
        {
            var absi = Invoke<int>(assembly, "abs", n);
            Assert.Equal(Math.Abs(n), absi);
        }
    }

    [Fact]
    public void RecursiveFactorial()
    {
        var assembly = Compile("""
            func fact(n: int32): int32 =
                if (n == 0) 1
                else n * fact(n - 1);
            """);

        var results = new[] { 1, 1, 2, 6, 24, 120, 720 };
        for (var i = 0; i < 7; ++i)
        {
            var facti = Invoke<int>(assembly, "fact", i);
            Assert.Equal(results[i], facti);
        }
    }

    [Fact]
    public void RecursiveFibonacci()
    {
        var assembly = Compile("""
            func fib(n: int32): int32 =
                if (n < 2) 1
                else fib(n - 1) + fib(n - 2);
            """);

        var results = new[] { 1, 1, 2, 3, 5, 8, 13, 21, 34, 55 };
        for (var i = 0; i < 10; ++i)
        {
            var fibi = Invoke<int>(assembly, "fib", i);
            Assert.Equal(results[i], fibi);
        }
    }

    [Fact]
    public void IterativeSum()
    {
        var assembly = Compile("""
            func sum(start: int32, end: int32): int32 {
                var i = start;
                var s = 0;
                while (i < end) {
                    i += 1;
                    s += i;
                }
                return s;
            }
            """);

        var results = new[] { 0, 1, 3, 6, 10, 15, 21, 28, 36, 45 };
        for (var i = 0; i < 10; ++i)
        {
            var sumi = Invoke<int>(assembly, "sum", 0, i);
            Assert.Equal(results[i], sumi);
        }
    }
}
