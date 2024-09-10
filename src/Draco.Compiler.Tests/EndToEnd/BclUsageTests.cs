using System.Numerics;
using static Draco.Compiler.Tests.TestUtilities;

namespace Draco.Compiler.Tests.EndToEnd;

[Collection(nameof(NoParallelizationCollectionDefinition))]
public sealed class BclUsageTests
{
    [Fact]
    public void HelloWorld()
    {
        var assembly = CompileToAssembly("""
            import System.Console;

            func main() {
                WriteLine("Hello, World!");
            }
            """);

        var stringWriter = new StringWriter();
        _ = Invoke<object?>(assembly: assembly, stdout: stringWriter);

        Assert.Equal($"Hello, World!{Environment.NewLine}", stringWriter.ToString(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Interpolation()
    {
        var assembly = CompileToAssembly("""
            import System.Console;

            func main() {
                Write("\{1} + \{2} = \{1 + 2}");
            }
            """);

        var stringWriter = new StringWriter();
        _ = Invoke<object?>(assembly: assembly, stdout: stringWriter);

        Assert.Equal("1 + 2 = 3", stringWriter.ToString());
    }

    [Fact]
    public void BasicStringBuilding()
    {
        var assembly = CompileToAssembly("""
            import System.Console;
            import System.Text;

            func main() {
                var sb = StringBuilder();
                sb.Append("Hello, ");
                sb.Append(123);
                sb.Append(true);
                sb.Append(" - and bye!");
                Write(sb.ToString());
            }
            """);

        var stringWriter = new StringWriter();
        _ = Invoke<object?>(assembly: assembly, stdout: stringWriter);

        Assert.Equal("Hello, 123True - and bye!", stringWriter.ToString());
    }

    [Fact]
    public void FullyQualifiedNames()
    {
        var assembly = CompileToAssembly("""
            import System.Console;
            import System.Text;

            func make_builder(): System.Text.StringBuilder = StringBuilder();

            func main() {
                var sb = make_builder();
                var myName = "Draco";
                sb.Append("Hello \{myName}!");
                Write(sb.ToString());
            }
            """);

        var stringWriter = new StringWriter();
        _ = Invoke<object?>(assembly: assembly, stdout: stringWriter);

        Assert.Equal("Hello Draco!", stringWriter.ToString());
    }

    [Fact]
    public void StackUsageWithExplicitGenerics()
    {
        var assembly = CompileToAssembly("""
            import System.Console;
            import System.Collections.Generic;

            func main() {
                val s = Stack<int32>();
                s.Push(1);
                s.Push(2);
                s.Push(3);
                Write(s.Pop());
                Write(s.Pop());
                Write(s.Pop());
            }
            """);

        var stringWriter = new StringWriter();
        _ = Invoke<object?>(assembly: assembly, stdout: stringWriter);

        Assert.Equal("321", stringWriter.ToString());
    }

    [Fact]
    public void StackUsageWithImplicitGenerics()
    {
        var assembly = CompileToAssembly("""
            import System.Console;
            import System.Collections.Generic;

            func main() {
                val s = Stack();
                s.Push(1);
                s.Push(2);
                s.Push(3);
                Write(s.Pop());
                Write(s.Pop());
                Write(s.Pop());
            }
            """);

        var stringWriter = new StringWriter();
        _ = Invoke<object?>(assembly: assembly, stdout: stringWriter);

        Assert.Equal("321", stringWriter.ToString());
    }

    [Fact]
    public void SystemTupleConstruction()
    {
        var assembly = CompileToAssembly("""
            import System.Tuple;

            func main() {
                val t = Create(1, "hello");
            }
            """);

        _ = Invoke<object?>(assembly: assembly);
    }

    [Fact]
    public void FullyQualifiedSystemTupleConstruction()
    {
        var assembly = CompileToAssembly("""
            func make(x: int32, y: int32): System.Tuple<int32, int32> =
                System.Tuple(x, y);
            """);

        var t = Invoke<Tuple<int, int>>(
            assembly: assembly,
            methodName: "make",
            args: [2, 3]);

        Assert.Equal(2, t.Item1);
        Assert.Equal(3, t.Item2);
    }

    [Fact]
    public void ListUsageWithPropertyAndIndexer()
    {
        var assembly = CompileToAssembly("""
            import System.Console;
            import System.Collections.Generic;
            
            func main() {
                var list = List();
                list.Add(0);
                list.Add(1);
                list.Add(2);
                var i = 0;
                while(i < list.Count){
                    list[i] *= 2;
                    Write(list[i]);
                    i += 1;
                }
            }
            """);

        var stringWriter = new StringWriter();
        _ = Invoke<object?>(assembly: assembly, stdout: stringWriter);

        Assert.Equal("024", stringWriter.ToString());
    }

    [Fact]
    public void NonGenericProperty()
    {
        var assembly = CompileToAssembly("""
            import System.Collections;
            import System.Console;

            func main() {
                Write(ArrayList().Count);
            }
            """);

        var stringWriter = new StringWriter();
        _ = Invoke<object?>(assembly: assembly, stdout: stringWriter);

        Assert.Equal("0", stringWriter.ToString());
    }

    [Fact]
    public void EnumeratingRange()
    {
        var assembly = CompileToAssembly("""
            import System.Console;
            import System.Linq.Enumerable;

            func main() {
                val enumerable = Range(0, 10);
                val enumerator = enumerable.GetEnumerator();
                while(enumerator.MoveNext())
                {
                    Write(enumerator.Current);
                }
            }
            """);

        var stringWriter = new StringWriter();
        _ = Invoke<object?>(assembly: assembly, stdout: stringWriter);

        Assert.Equal("0123456789", stringWriter.ToString());
    }

    [Fact]
    public void EnumeratingList()
    {
        var assembly = CompileToAssembly("""
            import System.Console;
            import System.Collections.Generic;

            func main() {
                val list = List();
                list.Add(2);
                list.Add(3);
                list.Add(5);
                val enumerator = list.GetEnumerator();
                while(enumerator.MoveNext())
                {
                    Write(enumerator.Current);
                }
            }
            """);

        var stringWriter = new StringWriter();
        _ = Invoke<object?>(assembly: assembly, stdout: stringWriter);

        Assert.Equal("235", stringWriter.ToString());
    }

    [Fact]
    public void StringifyInt()
    {
        var assembly = CompileToAssembly("""
            func stringify(n: int32): string = n.ToString();
            """);

        var result = Invoke<string>(
            assembly: assembly,
            methodName: "stringify",
            args: [123]);

        Assert.Equal("123", result);
    }

    [Fact]
    public void ParseInt()
    {
        var assembly = CompileToAssembly("""
            func parse(n: string): int32 = int32.Parse(n);
            """);

        var result = Invoke<int>(
            assembly: assembly,
            methodName: "parse",
            args: ["123"]);

        Assert.Equal(123, result);
    }

    [Fact]
    public void StringSplitting()
    {
        var assembly = CompileToAssembly("""
            import System;

            func foo(): string {
                val parts = "1, 2, 3".Split(",", StringSplitOptions.TrimEntries);
                return string.Join(";", parts);
            }
            """);

        var result = Invoke<string>(assembly: assembly, methodName: "foo");

        Assert.Equal("1;2;3", result);
    }

    [Fact]
    public void ForLoopSumming()
    {
        var assembly = CompileToAssembly("""
            import System.Collections.Generic;

            func sum(ns: IEnumerable<int32>): int32 {
                var s = 0;
                for (n in ns) s += n;
                return s;
            }
            """);

        var result = Invoke<int>(
            assembly: assembly,
            methodName: "sum",
            args: [new[] { 1, 1, 2, 3, 5, 8, 13 }]);

        Assert.Equal(33, result);
    }

    [Fact]
    public void ForLoopPrinting()
    {
        var assembly = CompileToAssembly("""
            import System.Collections.Generic;
            import System.Console;

            func log(ns: IEnumerable<int32>) {
                for (n in ns) Write(n);
            }
            """);

        var stringWriter = new StringWriter();
        _ = Invoke<object?>(
            assembly: assembly,
            methodName: "log",
            args: [new[] { 1, 1, 2, 3, 5, 8, 13 }],
            stdout: stringWriter);

        Assert.Equal("11235813", stringWriter.ToString());
    }

    [Fact]
    public void StringEqualsOperator()
    {
        var assembly = CompileToAssembly("""
            func streq(s1: string, s2: string): bool = s1 == s2;
            """);

        var case1 = Invoke<bool>(
            assembly: assembly,
            methodName: "streq",
            args: ["asd", "def"]);
        var case2 = Invoke<bool>(
            assembly: assembly,
            methodName: "streq",
            args: ["asd", "asd"]);

        Assert.False(case1);
        Assert.True(case2);
    }

    [Fact]
    public void StringAddOperator()
    {
        var assembly = CompileToAssembly("""
            func mangle(s1: string, s2: string): string {
                var result = "";
                result += s1 + s2;
                result += s2 + s1;
                return result;
            }
            """);

        var result = Invoke<string>(
            assembly: assembly,
            methodName: "mangle",
            args: ["asd", "def"]);

        Assert.Equal("asddefdefasd", result);
    }

    [Fact]
    public void NumericsVectorNegation()
    {
        var assembly = CompileToAssembly("""
            import System.Numerics;

            func neg(a: Vector2): Vector2 = -a;
            """);

        var result = Invoke<Vector2>(
            assembly: assembly,
            methodName: "neg",
            args: [new Vector2(1, 2)]);

        Assert.Equal(new Vector2(-1, -2), result);
    }

    [Fact]
    public void NumericsVectorAddition()
    {
        var assembly = CompileToAssembly("""
            import System.Numerics;

            func addthem(a: Vector2, b: Vector2): Vector2 = a + b;
            """);

        var result = Invoke<Vector2>(
            assembly: assembly,
            methodName: "addthem",
            args: [new Vector2(1, 2), new Vector2(6, 3)]);

        Assert.Equal(new Vector2(7, 5), result);
    }

    [Fact]
    public void NumericsVectorAdditionWithoutImportingNamespace()
    {
        var assembly = CompileToAssembly("""
            func addthem(a: System.Numerics.Vector2, b: System.Numerics.Vector2): System.Numerics.Vector2 = a + b;
            """);

        var result = Invoke<Vector2>(
            assembly: assembly,
            methodName: "addthem",
            args: [new Vector2(1, 2), new Vector2(6, 3)]);

        Assert.Equal(new Vector2(7, 5), result);
    }

    [Fact]
    public void NumericsVectorNegationWithoutImportingNamespace()
    {
        var assembly = CompileToAssembly("""
            func neg(a: System.Numerics.Vector2): System.Numerics.Vector2 = -a;
            """);

        var result = Invoke<Vector2>(
            assembly: assembly,
            methodName: "neg",
            args: [new Vector2(1, 2)]);

        Assert.Equal(new Vector2(-1, -2), result);
    }

    [Fact]
    public void AccessingConstants()
    {
        var assembly = CompileToAssembly("""
            import System.Math;

            func get_pi(): float64 = PI;
            """);

        var result = Invoke<double>(assembly: assembly, methodName: "get_pi");

        Assert.Equal(Math.PI, result);
    }

    [Fact]
    public void ChainedValueTypeMemberAccess()
    {
        var assembly = CompileToAssembly("""
            import System;

            func get_datetime_str(): string = DateTime.Now.TimeOfDay.ToString();
            """);

        var result = Invoke<string>(assembly: assembly, methodName: "get_datetime_str");

        Assert.NotNull(result);
    }
}
