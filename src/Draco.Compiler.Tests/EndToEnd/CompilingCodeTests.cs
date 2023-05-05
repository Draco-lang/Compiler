using static Draco.Compiler.Tests.ModuleTestsUtilities;

namespace Draco.Compiler.Tests.EndToEnd;

public sealed class CompilingCodeTests : EndToEndTestsBase
{
    [Fact]
    public void Max()
    {
        var assembly = Compile("""
            public func max(a: int32, b: int32): int32 = if (a > b) a else b;
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
            public func abs(n: int32): int32 = if (n > 0) n else -n;
            """);

        var inputs = new[] { 0, 1, -1, 3, 8, -3, -5 };
        foreach (var n in inputs)
        {
            var absi = Invoke<int>(assembly, "abs", n);
            Assert.Equal(Math.Abs(n), absi);
        }
    }

    [Fact]
    public void Between()
    {
        var assembly = Compile("""
            public func between(n: int32, a: int32, b: int32): bool = a <= n <= b;
            """);

        var trueInputs = new[] { (0, 0, 0), (0, 0, 1), (0, -1, 0), (0, 0, 5), (1, 0, 5), (4, 0, 5), (5, 0, 5) };
        foreach (var (n, a, b) in trueInputs)
        {
            var isBetween = Invoke<bool>(assembly, "between", n, a, b);
            Assert.True(isBetween);
        }

        var falseInputs = new[] { (-1, 0, 0), (2, 0, 1), (1, -1, 0), (6, 0, 5), (-1, 0, 5), (10, 0, 5), (7, 0, 5) };
        foreach (var (n, a, b) in falseInputs)
        {
            var isBetween = Invoke<bool>(assembly, "between", n, a, b);
            Assert.False(isBetween);
        }
    }

    [Fact]
    public void Negate()
    {
        var assembly = Compile("""
            public func negate(n: int32): int32 = if (n < 0) n else -n;
            """);

        var inputs = new[] { 0, 1, -1, 3, 8, -3, -5 };
        foreach (var n in inputs)
        {
            var neg = Invoke<int>(assembly, "negate", n);
            Assert.Equal(n < 0 ? n : -n, neg);
        }
    }

    [Fact]
    public void Power()
    {
        var assembly = Compile("""
            public func power(n: int32, exponent: int32): int32 = {
                var i = 1;
                var result = n;
                while (i < exponent){
                    result *= n;
                    i += 1;
                }
                result
            };
            """);

        var inputs = new[] { (1, 3), (5, 2), (-2, 4), (3, 3), (8, 2) };
        foreach (var (n, exp) in inputs)
        {
            var pow = Invoke<int>(assembly, "power", n, exp);
            Assert.Equal(Math.Pow(n, exp), pow);
        }
    }

    [Fact]
    public void PowerWithFloat64()
    {
        var assembly = Compile("""
            public func power(n: float64, exponent: int32): float64 = {
                var i = 1;
                var result = n;
                while (i < exponent){
                    result *= n;
                    i += 1;
                }
                result
            };
            """);

        var inputs = new[] { (1.5, 3), (5.28, 2), (-2.5, 4), (3, 3), (8.847, 2) };
        foreach (var (n, exp) in inputs)
        {
            var pow = Invoke<double>(assembly, "power", n, exp);
            Assert.Equal(Math.Pow(n, exp), pow, 5);
        }
    }

    [Fact]
    public void LazyAnd()
    {
        var assembly = Compile("""
            public func foo(nx2: bool, nx3: bool): int32 = {
                var result = 1;
                nx2 and { result *= 2; nx3 } and { result *= 3; false };
                result
            };
            """);

        var r1 = Invoke<int>(assembly, "foo", false, false);
        var r2 = Invoke<int>(assembly, "foo", false, true);
        var r3 = Invoke<int>(assembly, "foo", true, false);
        var r4 = Invoke<int>(assembly, "foo", true, true);

        Assert.Equal(1, r1);
        Assert.Equal(1, r2);
        Assert.Equal(2, r3);
        Assert.Equal(6, r4);
    }

    [Fact]
    public void LazyOr()
    {
        var assembly = Compile("""
            public func foo(nx2: bool, nx3: bool): int32 = {
                var result = 1;
                nx2 or { result *= 2; nx3 } or { result *= 3; false };
                result
            };
            """);

        var r1 = Invoke<int>(assembly, "foo", false, false);
        var r2 = Invoke<int>(assembly, "foo", false, true);
        var r3 = Invoke<int>(assembly, "foo", true, false);
        var r4 = Invoke<int>(assembly, "foo", true, true);

        Assert.Equal(6, r1);
        Assert.Equal(2, r2);
        Assert.Equal(1, r3);
        Assert.Equal(1, r4);
    }

    [Fact]
    public void RecursiveFactorial()
    {
        var assembly = Compile("""
            public func fact(n: int32): int32 =
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
            public func fib(n: int32): int32 =
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
            public func sum(start: int32, end: int32): int32 {
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

    [Fact]
    public void Globals()
    {
        var assembly = Compile("""
            var x = 0;
            func bar() { x += 1; }
            public func foo(): int32 {
                bar();
                bar();
                bar();
                return x;
            }
            """);

        var x = Invoke<int>(assembly, "foo");
        Assert.Equal(3, x);
    }

    [Fact]
    public void NonzeroGlobals()
    {
        var assembly = Compile("""
            var x = 123;
            func bar() { x += 1; }
            public func foo(): int32 {
                bar();
                bar();
                bar();
                return x;
            }
            """);

        var x = Invoke<int>(assembly, "foo");
        Assert.Equal(126, x);
    }

    [Fact]
    public void ComplexInitializerGlobals()
    {
        var assembly = Compile("""
            public func foo(): int32 = x;
            var x = add(1, 2) + 1 + 2 + 3;
            func add(x: int32, y: int32): int32 = 2 * (x + y);
            """);

        var x = Invoke<int>(assembly, "foo");
        Assert.Equal(12, x);
    }

    [Fact]
    public void BreakAndContinue()
    {
        var assembly = Compile("""
            public func foo(): int32 {
                var s = 0;
                var i = 0;
                while (true) {
                    if (s > 30) goto break;
                    i += 1;
                    if (i mod 2 == 1) goto continue;
                    s += i;
                }
                return s;
            }
            """);

        var x = Invoke<int>(assembly, "foo");
        Assert.Equal(42, x);
    }

    [Fact]
    public void MultiLineStringCutoff()
    {
        var assembly = Compile(""""
            public func foo(): string{
                return """
                Hello
                    World!
                """;
            }
            """");

        var x = Invoke<string>(assembly, "foo");
        Assert.Equal("""
            Hello
                World!
            """, x);
    }

    [Fact]
    public void MultiLineStringInterpolation()
    {
        var assembly = Compile(""""
            public func foo(): string{
                return """
                Hello \{1 + 2} World!
                """;
            }
            """");

        var x = Invoke<string>(assembly, "foo");
        Assert.Equal("Hello 3 World!", x);
    }

    [Fact]
    public void MultiLineStringLineContinuation()
    {
        var assembly = Compile(""""
            public func foo(): string{
                return """
                Hello\
                    World!
                """;
            }
            """");

        var x = Invoke<string>(assembly, "foo");
        Assert.Equal("Hello    World!", x);
    }

    [Fact]
    public void SimpleModuleFunctionCallExample()
    {
        var bar = CreateSyntaxTree("""
            public func bar(): int32{
                return FooTest.foo();
            }
            """, ToPath("Tests", "bar.draco"));

        var foo = CreateSyntaxTree("""
            internal func foo(): int32 = x;
            val x = 5;
            """, ToPath("Tests", "FooTest", "foo.draco"));

        var assembly = Compile(ToPath("Tests"), bar, foo);

        var x = Invoke<int>(assembly, "Tests", "bar");
        Assert.Equal(5, x);
    }

    [Fact]
    public void SimpleModuleGlobalAccessExample()
    {
        var bar = CreateSyntaxTree("""
            public func bar(): int32{
                return FooTest.x;
            }
            """, ToPath("Tests", "bar.draco"));

        var foo = CreateSyntaxTree("""
            public val x = 5;
            """, ToPath("Tests", "FooTest", "foo.draco"));

        var assembly = Compile(ToPath("Tests"), bar, foo);

        var x = Invoke<int>(assembly, "Tests", "bar");
        Assert.Equal(5, x);
    }

    [Fact]
    public void SimpleNestedModuleAccessExample()
    {
        var foo = CreateSyntaxTree("""
            public func foo(): int32 = 5;
            """, ToPath("Tests", "FooTest", "foo.draco"));

        var assembly = Compile(ToPath("Tests"), foo);

        var x = Invoke<int>(assembly, "Tests.FooTest", "foo");
        Assert.Equal(5, x);
    }
}
