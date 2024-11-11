using Draco.Compiler.Api.Syntax;
using static Draco.Compiler.Tests.TestUtilities;

namespace Draco.Compiler.Tests.EndToEnd;

public sealed class CompilingCodeTests
{
    [Fact]
    public void Max()
    {
        var assembly = CompileToAssembly("""
            func max(a: int32, b: int32): int32 = if (a > b) a else b;
            """);

        var inputs = new[] { (0, 0), (1, 0), (0, 1), (5, 0), (5, 4), (4, 5) };
        foreach (var (a, b) in inputs)
        {
            var maxi = Invoke<int>(
                assembly: assembly,
                methodName: "max",
                args: [a, b]);
            Assert.Equal(Math.Max(a, b), maxi);
        }
    }

    [Fact]
    public void Abs()
    {
        var assembly = CompileToAssembly("""
            func abs(n: int32): int32 = if (n > 0) n else -n;
            """);

        var inputs = new[] { 0, 1, -1, 3, 8, -3, -5 };
        foreach (var n in inputs)
        {
            var absi = Invoke<int>(
                assembly: assembly,
                methodName: "abs",
                args: [n]);
            Assert.Equal(Math.Abs(n), absi);
        }
    }

    [Fact]
    public void Between()
    {
        var assembly = CompileToAssembly("""
            func between(n: int32, a: int32, b: int32): bool = a <= n <= b;
            """);

        var trueInputs = new[] { (0, 0, 0), (0, 0, 1), (0, -1, 0), (0, 0, 5), (1, 0, 5), (4, 0, 5), (5, 0, 5) };
        foreach (var (n, a, b) in trueInputs)
        {
            var isBetween = Invoke<bool>(
                assembly: assembly,
                methodName: "between",
                args: [n, a, b]);
            Assert.True(isBetween);
        }

        var falseInputs = new[] { (-1, 0, 0), (2, 0, 1), (1, -1, 0), (6, 0, 5), (-1, 0, 5), (10, 0, 5), (7, 0, 5) };
        foreach (var (n, a, b) in falseInputs)
        {
            var isBetween = Invoke<bool>(
                assembly: assembly,
                methodName: "between",
                args: [n, a, b]);
            Assert.False(isBetween);
        }
    }

    [Fact]
    public void Negate()
    {
        var assembly = CompileToAssembly("""
            func negate(n: int32): int32 = if (n < 0) n else -n;
            """);

        var inputs = new[] { 0, 1, -1, 3, 8, -3, -5 };
        foreach (var n in inputs)
        {
            var neg = Invoke<int>(
                assembly: assembly,
                methodName: "negate",
                args: [n]);
            Assert.Equal(n < 0 ? n : -n, neg);
        }
    }

    [Fact]
    public void Power()
    {
        var assembly = CompileToAssembly("""
            func power(n: int32, exponent: int32): int32 = {
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
            var pow = Invoke<int>(
                assembly: assembly,
                methodName: "power",
                args: [n, exp]);
            Assert.Equal(Math.Pow(n, exp), pow);
        }
    }

    [Fact]
    public void PowerWithFloat64()
    {
        var assembly = CompileToAssembly("""
            func power(n: float64, exponent: int32): float64 = {
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
            var pow = Invoke<double>(
                assembly: assembly,
                methodName: "power",
                args: [n, exp]);
            Assert.Equal(Math.Pow(n, exp), pow, 5);
        }
    }

    [Fact]
    public void LazyAnd()
    {
        var assembly = CompileToAssembly("""
            func foo(nx2: bool, nx3: bool): int32 = {
                var result = 1;
                nx2 and { result *= 2; nx3 } and { result *= 3; false };
                result
            };
            """);

        var r1 = Invoke<int>(assembly: assembly, methodName: "foo", args: [false, false]);
        var r2 = Invoke<int>(assembly: assembly, methodName: "foo", args: [false, true]);
        var r3 = Invoke<int>(assembly: assembly, methodName: "foo", args: [true, false]);
        var r4 = Invoke<int>(assembly: assembly, methodName: "foo", args: [true, true]);

        Assert.Equal(1, r1);
        Assert.Equal(1, r2);
        Assert.Equal(2, r3);
        Assert.Equal(6, r4);
    }

    [Fact]
    public void LazyOr()
    {
        var assembly = CompileToAssembly("""
            func foo(nx2: bool, nx3: bool): int32 = {
                var result = 1;
                nx2 or { result *= 2; nx3 } or { result *= 3; false };
                result
            };
            """);

        var r1 = Invoke<int>(assembly: assembly, methodName: "foo", args: [false, false]);
        var r2 = Invoke<int>(assembly: assembly, methodName: "foo", args: [false, true]);
        var r3 = Invoke<int>(assembly: assembly, methodName: "foo", args: [true, false]);
        var r4 = Invoke<int>(assembly: assembly, methodName: "foo", args: [true, true]);

        Assert.Equal(6, r1);
        Assert.Equal(2, r2);
        Assert.Equal(1, r3);
        Assert.Equal(1, r4);
    }

    [Fact]
    public void RecursiveFactorial()
    {
        var assembly = CompileToAssembly("""
            func fact(n: int32): int32 =
                if (n == 0) 1
                else n * fact(n - 1);
            """);

        var results = new[] { 1, 1, 2, 6, 24, 120, 720 };
        for (var i = 0; i < 7; ++i)
        {
            var facti = Invoke<int>(
                assembly: assembly,
                methodName: "fact",
                args: [i]);
            Assert.Equal(results[i], facti);
        }
    }

    [Fact]
    public void RecursiveFibonacci()
    {
        var assembly = CompileToAssembly("""
            func fib(n: int32): int32 =
                if (n < 2) 1
                else fib(n - 1) + fib(n - 2);
            """);

        var results = new[] { 1, 1, 2, 3, 5, 8, 13, 21, 34, 55 };
        for (var i = 0; i < 10; ++i)
        {
            var fibi = Invoke<int>(
                assembly: assembly,
                methodName: "fib",
                args: [i]);
            Assert.Equal(results[i], fibi);
        }
    }

    [Fact]
    public void IterativeSum()
    {
        var assembly = CompileToAssembly("""
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
            var sumi = Invoke<int>(
                assembly: assembly,
                methodName: "sum",
                args: [0, i]);
            Assert.Equal(results[i], sumi);
        }
    }

    [Fact]
    public void Globals()
    {
        var assembly = CompileToAssembly("""
            var x = 0;
            func bar() { x += 1; }
            func foo(): int32 {
                bar();
                bar();
                bar();
                return x;
            }
            """);

        var x = Invoke<int>(assembly: assembly, methodName: "foo");

        Assert.Equal(3, x);
    }

    [Fact]
    public void NonzeroGlobals()
    {
        var assembly = CompileToAssembly("""
            var x = 123;
            func bar() { x += 1; }
            func foo(): int32 {
                bar();
                bar();
                bar();
                return x;
            }
            """);

        var x = Invoke<int>(assembly: assembly, methodName: "foo");

        Assert.Equal(126, x);
    }

    [Fact]
    public void ComplexInitializerGlobals()
    {
        var assembly = CompileToAssembly("""
            func foo(): int32 = x;
            var x = add(1, 2) + 1 + 2 + 3;
            func add(x: int32, y: int32): int32 = 2 * (x + y);
            """);

        var x = Invoke<int>(assembly: assembly, methodName: "foo");

        Assert.Equal(12, x);
    }

    [Fact]
    public void BreakAndContinue()
    {
        var assembly = CompileToAssembly("""
            func foo(): int32 {
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

        var x = Invoke<int>(assembly: assembly, methodName: "foo");

        Assert.Equal(42, x);
    }

    [Fact]
    public void MultiLineStringCutoff()
    {
        var assembly = CompileToAssembly(""""
            func foo(): string{
                return """
                Hello
                    World!
                """;
            }
            """");

        var x = Invoke<string>(assembly: assembly, methodName: "foo");

        Assert.Equal("""
            Hello
                World!
            """, x);
    }

    [Fact]
    public void MultiLineStringInterpolation()
    {
        var assembly = CompileToAssembly(""""
            func foo(): string{
                return """
                Hello \{1 + 2} World!
                """;
            }
            """");

        var x = Invoke<string>(assembly: assembly, methodName: "foo");

        Assert.Equal("Hello 3 World!", x);
    }

    [Fact]
    public void MultiLineStringLineContinuation()
    {
        var assembly = CompileToAssembly(""""
            func foo(): string{
                return """
                Hello\
                    World!
                """;
            }
            """");

        var x = Invoke<string>(assembly: assembly, methodName: "foo");

        Assert.Equal("Hello    World!", x);
    }

    [Fact]
    public void FunctionsWithExplicitGenerics()
    {
        var assembly = CompileToAssembly(""""
            func identity<T>(x: T): T = x;
            func first<T, U>(a: T, b: U): T = identity<T>(a);
            func second<T, U>(a: T, b: U): U = identity<U>(b);

            func foo(n: int32, m: int32): int32 =
                first<int32, string>(n, "Hello") + second<bool, int32>(false, m);
            """");

        var x = Invoke<int>(
            assembly: assembly,
            methodName: "foo",
            args: [2, 3]);

        Assert.Equal(5, x);
    }

    [Fact]
    public void FunctionsWithImplicitGenerics()
    {
        var assembly = CompileToAssembly(""""
            func identity<T>(x: T): T = x;
            func first<T, U>(a: T, b: U): T = identity(a);
            func second<T, U>(a: T, b: U): U = identity(b);

            func foo(n: int32, m: int32): int32 =
                first(n, "Hello") + second(false, m);
            """");

        var x = Invoke<int>(
            assembly: assembly,
            methodName: "foo",
            args: [2, 3]);

        Assert.Equal(5, x);
    }

    [Fact]
    public void ModuleFunctionCall()
    {
        var bar = SyntaxTree.Parse("""
            public func bar(): int32{
                return FooTest.foo();
            }
            """, ToPath("Tests", "bar.draco"));

        var foo = SyntaxTree.Parse("""
            internal func foo(): int32 = x;
            val x = 5;
            """, ToPath("Tests", "FooTest", "foo.draco"));

        var assembly = CompileToAssembly(
            syntaxTrees: [bar, foo],
            rootModulePath: ToPath("Tests"));

        var x = Invoke<int>(
            assembly: assembly,
            moduleName: "Tests",
            methodName: "bar");

        Assert.Equal(5, x);
    }

    [Fact]
    public void ModuleGlobalAccess()
    {
        var bar = SyntaxTree.Parse("""
            public func bar(): int32{
                return FooTest.x;
            }
            """, ToPath("Tests", "bar.draco"));

        var foo = SyntaxTree.Parse("""
            public val x = 5;
            """, ToPath("Tests", "FooTest", "foo.draco"));

        var assembly = CompileToAssembly(
            syntaxTrees: [bar, foo],
            rootModulePath: ToPath("Tests"));

        var x = Invoke<int>(
            assembly: assembly,
            moduleName: "Tests",
            methodName: "bar");

        Assert.Equal(5, x);
    }

    [Fact]
    public void NestedModuleAccess()
    {
        var foo = SyntaxTree.Parse("""
            public func foo(): int32 = 5;
            """, ToPath("Tests", "FooTest", "foo.draco"));

        var assembly = CompileToAssembly(
            syntaxTrees: [foo],
            rootModulePath: ToPath("Tests"));

        var x = Invoke<int>(
            assembly: assembly,
            moduleName: "Tests.FooTest",
            methodName: "foo");

        Assert.Equal(5, x);
    }

    [Fact]
    public void GenericMemberMethodCall()
    {
        var csReference = CompileCSharpToStream("""
            public class IdentityProvider
            {
                public T Identity<T>(T x) => x;
            }
            """);
        var assembly = CompileToAssembly("""
            func foo(): int32 {
                val provider = IdentityProvider();
                return provider.Identity<int32>(2) + provider.Identity(123);
            }
            """,
            additionalReferences: [csReference]);

        var x = Invoke<int>(assembly: assembly, methodName: "foo");

        Assert.Equal(125, x);
    }

    [Fact]
    public void PropertiesCompoundAssignment()
    {
        var csReference = CompileCSharpToStream("""
            public class FooTest
            {
                public static int StaticProp { get; set; } = 5;
                public int NonStaticProp { get; set; } = 4;
            }
            """);
        var assembly = CompileToAssembly("""
            func foo(): int32 {
                var test = FooTest();
                test.NonStaticProp += 2;
                FooTest.StaticProp += 3;
                return test.NonStaticProp += FooTest.StaticProp;
            }
            """,
            additionalReferences: [csReference]);

        var x = Invoke<int>(assembly: assembly, methodName: "foo");

        Assert.Equal(14, x);
    }

    [Fact]
    public void MemberFields()
    {
        var csReference = CompileCSharpToStream("""
            public class FooTest
            {
                public int number = 3;
            }
            """);
        var assembly = CompileToAssembly("""
            func foo(): int32 {
                var test = FooTest();
                test.number += 2;
                return test.number;
            }
            """,
            additionalReferences: [csReference]);

        var x = Invoke<int>(assembly: assembly, methodName: "foo");

        Assert.Equal(5, x);
    }

    [Fact]
    public void StaticFields()
    {
        var csReference = CompileCSharpToStream(
            """
            public class FooTest
            {
                public static int number = 3;
            }
            """);
        var assembly = CompileToAssembly("""
            func foo(): int32 {
                FooTest.number += 2;
                return FooTest.number;
            }
            """,
            additionalReferences: [csReference]);

        var x = Invoke<int>(assembly: assembly, methodName: "foo");

        Assert.Equal(5, x);
    }

    [Fact]
    public void MergeArrays()
    {
        var assembly = CompileToAssembly("""
            func merge<T>(a: Array<T>, b: Array<T>): Array<T> {
                val result = Array(a.Length + b.Length);
                var offs = 0;
                while (offs < a.Length) {
                    result[offs] = a[offs];
                    offs += 1;
                }
                while (offs - a.Length < b.Length) {
                    result[offs] = b[offs - a.Length];
                    offs += 1;
                }
                return result;
            }

            func merge_int(a: Array<int32>, b: Array<int32>): Array<int32> = merge(a, b);
            """);

        var input1 = new[] { 1, 3 };
        var input2 = new[] { 2, 4 };
        var expectedOutput = new[] { 1, 3, 2, 4 };
        var output = Invoke<int[]>(
            assembly: assembly,
            methodName: "merge_int",
            args: [input1, input2]);

        Assert.True(output.SequenceEqual(expectedOutput));
    }

    [Fact]
    public void BubbleSortArray()
    {
        var assembly = CompileToAssembly("""
            func bubblesort(a: Array<int32>) {
                var i = 1;
                while (i < a.Length) {
                    var j = 0;
                    while (j < a.Length - i) {
                        if (a[j + 1] < a[j]) {
                            val tmp = a[j];
                            a[j] = a[j + 1];
                            a[j + 1] = tmp;
                        }
                        j += 1;
                    }
                    i += 1;
                }
            }
            """);

        var input = new[] { 17, 1, 12, 7, 10, 10, 6, 12, 2, 8 };
        var output = new[] { 1, 2, 6, 7, 8, 10, 10, 12, 12, 17 };

        Invoke<object?>(
            assembly: assembly,
            methodName: "bubblesort",
            args: [input]);

        Assert.True(input.SequenceEqual(output));
    }

    [Fact]
    public void InCodeModuleUsage()
    {
        var assembly = CompileToAssembly(""""
            func foo(): string = FooModule.GetFoo();

            module FooModule{
                public func GetFoo(): string = "foo";
            }
            """");

        var x = Invoke<string>(assembly: assembly, methodName: "foo");

        Assert.Equal("foo", x);
    }

    [Fact]
    public void InCodeModuleUsageImportingInsideModule()
    {
        var assembly = CompileToAssembly(""""
            func foo(): string = FooModule.Hello();

            module FooModule {
                import System.Text;

                public func Hello(): string{
                    var sb = StringBuilder();
                    sb.Append("Hello, World!");
                    return sb.ToString();
                }
            }
            """");

        var x = Invoke<string>(assembly: assembly, methodName: "foo");

        Assert.Equal("Hello, World!", x);
    }

    [Fact]
    public void VariadicArgsSum()
    {
        var assembly = CompileToAssembly(""""
            func sum(...ns: Array<int32>): int32 {
                var result = 0;
                var i = 0;
                while (i < ns.Length) {
                    result += ns[i];
                    i += 1;
                }
                return result;
            }

            func get_sum(): int32 = sum(1, 2, 3, 4, 5);
            """");

        var x = Invoke<int>(assembly: assembly, methodName: "get_sum");

        Assert.Equal(15, x);
    }

    [Fact]
    public void MultidimensionalArrays()
    {
        var assembly = CompileToAssembly(""""
            func make(a: int32, b: int32, c: int32, d: int32): Array2D<int32> {
                val res = Array2D(2, 2);
                res[0, 0] = a;
                res[1, 0] = b;
                res[0, 1] = c;
                res[1, 1] = d;
                return res;
            }

            func add_to_main_diagonal(a: Array2D<int32>, n: int32) {
                a[0, 0] += n;
                a[1, 1] += n;
            }

            func sum(a: Array2D<int32>): int32 = a[0, 0] + a[1, 0] + a[0, 1] + a[1, 1];

            func get_result(): int32 {
                val m = make(1, 2, 3, 4);
                add_to_main_diagonal(m, 7);
                return sum(m);
            }
            """");

        var x = Invoke<int>(assembly: assembly, methodName: "get_result");

        Assert.Equal(24, x);
    }

    [Fact]
    public void SingleInterpolatedElement()
    {
        var assembly = CompileToAssembly(""""
            func stringify(a: int32): string = "\{a}";
            """");

        var x = Invoke<string>(
            assembly: assembly,
            methodName: "stringify",
            args: [123]);

        Assert.Equal("123", x);
    }

    [Fact]
    public void PlusIntegerCompiles()
    {
        var assembly = CompileToAssembly("""
            func zero(): int32 = +0;
            """);

        var x = Invoke<int>(assembly: assembly, methodName: "zero");

        Assert.Equal(0, x);
    }

    [Fact]
    public void ArrayCreationWithGenericArgument() => CompileToAssembly("""
        func main() {
            var memory = Array<int32>(1);
            memory[0] = 1;
            memory[0] = 1;
        }
        """);

    [Fact]
    public void DefaultValueIntrinsic()
    {
        var assembly = CompileToAssembly("""
            func nullObj(): object = default<object>();
            func zeroInt(): int32 = default<int32>();
            """);

        var nullObj = Invoke<object>(assembly: assembly, methodName: "nullObj");
        var zeroInt = Invoke<int>(assembly: assembly, methodName: "zeroInt");

        Assert.Null(nullObj);
        Assert.Equal(0, zeroInt);
    }

    [Fact]
    public void FuncDelegates()
    {
        var assembly = CompileToAssembly("""
            import System;

            func getValue(): int32 = getValueImpl(add);
            func add(a: int32, b: int32): int32 = a + b;
            func getValueImpl(f: Func<int32, int32, int32>): int32 = f(2, 3);
            """);

        var result = Invoke<int>(assembly: assembly, methodName: "getValue");

        Assert.Equal(5, result);
    }

    [Fact]
    public void ActionDelegates()
    {
        var assembly = CompileToAssembly("""
            import System;
            import System.Console;

            func call() = invokeImpl(printThem);
            func printThem(a: string, b: string) = Write(a + ", " + b);
            func invokeImpl(f: Action<string, string>) = f("Hello", "World");
            """);

        var stringWriter = new StringWriter();
        _ = Invoke<object?>(
            assembly: assembly,
            methodName: "call",
            stdout: stringWriter);

        Assert.Equal("Hello, World", stringWriter.ToString());
    }

    [Fact]
    public void ParameterlessActionDelegates()
    {
        var assembly = CompileToAssembly("""
            import System;
            import System.Console;

            func call() = invokeImpl(printHello);
            func printHello() = Write("Hello, World!");
            func invokeImpl(f: Action) = f();
            """);

        var stringWriter = new StringWriter();
        _ = Invoke<object?>(
            assembly: assembly,
            methodName: "call",
            stdout: stringWriter);

        Assert.Equal("Hello, World!", stringWriter.ToString());
    }

    [Fact]
    public void GlobalInferredFromBlock()
    {
        var assembly = CompileToAssembly("""
            import System.Collections.Generic;

            val l = {
                val l = List();
                l.Add(1);
                l
            };

            func get_l(): List<int32> = l;
            """);

        var l = Invoke<List<int>>(assembly: assembly, methodName: "get_l");

        Assert.Single(l);
        Assert.Equal(1, l[0]);
    }

    [Fact]
    public void EnumEqualityOperators()
    {
        var assembly = CompileToAssembly("""
            import System;

            func equate(a: StringComparison, b: StringComparison): bool = a == b;
            func inequate(a: StringComparison, b: StringComparison): bool = a != b;
            """);

        var eq1 = Invoke<bool>(
            assembly: assembly,
            methodName: "equate",
            args: [StringComparison.Ordinal, StringComparison.Ordinal]);
        var eq2 = Invoke<bool>(
            assembly: assembly,
            methodName: "equate",
            args: [StringComparison.Ordinal, StringComparison.OrdinalIgnoreCase]);
        var neq1 = Invoke<bool>(
            assembly: assembly,
            methodName: "inequate",
            args: [StringComparison.Ordinal, StringComparison.Ordinal]);
        var neq2 = Invoke<bool>(
            assembly: assembly,
            methodName: "inequate",
            args: [StringComparison.Ordinal, StringComparison.OrdinalIgnoreCase]);

        Assert.True(eq1);
        Assert.False(eq2);
        Assert.False(neq1);
        Assert.True(neq2);
    }

    [Fact]
    public void IndexSetReturnsValue()
    {
        var assembly = CompileToAssembly("""
            import System.Collections.Generic;

            func setItemTo42(l: List<int32>): int32 {
                val a = (l[0] = 42);
                return a;
            }
            """);

        var l = new List<int> { 1, 2, 3 };
        var a = Invoke<int>(
            assembly: assembly,
            methodName: "setItemTo42",
            args: [l]);

        Assert.Equal(42, l[0]);
        Assert.Equal(42, a);
    }

    [Fact]
    public void IndexerEvaluationOrder()
    {
        var assembly = CompileToAssembly("""
            import System.Console;
            import System.Collections.Generic;

            func doIt() {
                val l = List();
                l.Add(1);
                l[{ WriteLine("A"); 0 }] = { WriteLine("B"); 2 };
            }
            """);

        var stringWriter = new StringWriter();
        _ = Invoke<object?>(
            assembly: assembly,
            methodName: "doIt",
            stdout: stringWriter);

        Assert.Equal("A\nB\n", stringWriter.ToString(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void RelationalOperatorIsShortCircuiting()
    {
        var assembly = CompileToAssembly("""
            import System.Console;

            func doCompare(): bool = { WriteLine("A"); 1 } > { WriteLine("B"); 2 } > { WriteLine("C"); 3 };
            """);

        var stringWriter = new StringWriter();
        var result = Invoke<bool>(
            assembly: assembly,
            methodName: "doCompare",
            stdout: stringWriter);

        Assert.False(result);
        Assert.Equal("A\nB\n", stringWriter.ToString(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void ForLoopOverArray()
    {
        var assembly = CompileToAssembly("""
            func concat(os: Array<object>): string {
                var result = System.Text.StringBuilder();
                for (o in os) result.Append(o);
                return result.ToString();
            }
            """);

        var result = Invoke<string>(
            assembly: assembly,
            methodName: "concat",
            args: [new object[] { "Hello", ", ", "World!" }]);

        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public void LocalFunctions()
    {
        var assembly = CompileToAssembly("""
            func outer(x: int32): int32 {
                func inner(x: int32, y: int32): int32 = x + y;

                return inner(x, 1);
            }
            """);

        var result = Invoke<int>(
            assembly: assembly,
            methodName: "outer",
            args: [2]);

        Assert.Equal(3, result);
    }

    [Fact]
    public void ClassHelloWorld()
    {
        var assembly = CompileToAssembly("""
            import System.Console;

            class Foo {
                func bar() {
                    WriteLine("Hello, World!");
                }
            }
            """);

        var stringWriter = new StringWriter();
        _ = Invoke<object?>(assembly: assembly, stdout: stringWriter, methodName: "bar", moduleName: "FreeFunctions.Foo");

        Assert.Equal($"Hello, World!{Environment.NewLine}", stringWriter.ToString(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void InstanceField()
    {
        var assembly = CompileToAssembly("""
            import System.Console;

            func bar(): int32 {
                var foo = Foo();
                foo.increment();
                return foo.get();
            }

            class Foo {
                field var i: int32;

                public func increment(this) {
                    this.i += 1;
                }

                public func get(this): int32 {
                    return this.i;
                }
            }
            """);

        var value = Invoke<int>(assembly: assembly, methodName: "bar");
        Assert.Equal(1, value);
    }
}
