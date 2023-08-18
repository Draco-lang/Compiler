namespace Draco.Compiler.Tests.EndToEnd;

[Collection(nameof(NoParallelizationCollectionDefinition))]
public sealed class BclUsageTests : EndToEndTestsBase
{
    [Fact]
    public void HelloWorld()
    {
        var assembly = Compile("""
            import System.Console;

            public func main() {
                WriteLine("Hello, World!");
            }
            """);

        var stringWriter = new StringWriter();
        _ = Invoke<object?>(
            assembly: assembly,
            methodName: "main",
            stdin: null,
            stdout: stringWriter);

        Assert.Equal($"Hello, World!{Environment.NewLine}", stringWriter.ToString(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Interpolation()
    {
        var assembly = Compile("""
            import System.Console;

            public func main() {
                Write("\{1} + \{2} = \{1 + 2}");
            }
            """);

        var stringWriter = new StringWriter();
        _ = Invoke<object?>(
            assembly: assembly,
            methodName: "main",
            stdin: null,
            stdout: stringWriter);

        Assert.Equal("1 + 2 = 3", stringWriter.ToString());
    }

    [Fact]
    public void BasicStringBuilding()
    {
        var assembly = Compile("""
            import System.Console;
            import System.Text;

            public func main() {
                var sb = StringBuilder();
                sb.Append("Hello, ");
                sb.Append(123);
                sb.Append(true);
                sb.Append(" - and bye!");
                Write(sb.ToString());
            }
            """);

        var stringWriter = new StringWriter();
        _ = Invoke<object?>(
            assembly: assembly,
            methodName: "main",
            stdin: null,
            stdout: stringWriter);

        Assert.Equal("Hello, 123True - and bye!", stringWriter.ToString());
    }

    [Fact]
    public void FullyQualifiedNames()
    {
        var assembly = Compile("""
            import System.Console;
            import System.Text;

            func make_builder(): System.Text.StringBuilder = StringBuilder();

            public func main() {
                var sb = make_builder();
                var myName = "Draco";
                sb.Append("Hello \{myName}!");
                Write(sb.ToString());
            }
            """);

        var stringWriter = new StringWriter();
        _ = Invoke<object?>(
            assembly: assembly,
            methodName: "main",
            stdin: null,
            stdout: stringWriter);

        Assert.Equal("Hello Draco!", stringWriter.ToString());
    }

    [Fact]
    public void StackUsageWithExplicitGenerics()
    {
        var assembly = Compile("""
            import System.Console;
            import System.Collections.Generic;

            public func main() {
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
        _ = Invoke<object?>(
            assembly: assembly,
            methodName: "main",
            stdin: null,
            stdout: stringWriter);

        Assert.Equal("321", stringWriter.ToString());
    }

    [Fact]
    public void StackUsageWithImplicitGenerics()
    {
        var assembly = Compile("""
            import System.Console;
            import System.Collections.Generic;

            public func main() {
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
        _ = Invoke<object?>(
            assembly: assembly,
            methodName: "main",
            stdin: null,
            stdout: stringWriter);

        Assert.Equal("321", stringWriter.ToString());
    }

    [Fact]
    public void SystemTupleConstruction()
    {
        var assembly = Compile("""
            import System.Tuple;

            public func main() {
                val t = Create(1, "hello");
            }
            """);

        _ = Invoke<object?>(
            assembly: assembly,
            methodName: "main",
            stdin: null,
            stdout: null);
    }

    [Fact]
    public void FullyQualifiedSystemTupleConstruction()
    {
        var assembly = Compile("""
            public func make(x: int32, y: int32): System.Tuple<int32, int32> =
                System.Tuple(x, y);
            """);

        var t = Invoke<Tuple<int, int>>(
            assembly: assembly,
            methodName: "make",
            args: new object[] { 2, 3 },
            stdin: null,
            stdout: null);
        Assert.Equal(2, t.Item1);
        Assert.Equal(3, t.Item2);
    }

    [Fact]
    public void ListUsageWithPropertyAndIndexer()
    {
        var assembly = Compile("""
            import System.Console;
            import System.Collections.Generic;
            
            public func main() {
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
        _ = Invoke<object?>(
            assembly: assembly,
            methodName: "main",
            stdin: null,
            stdout: stringWriter);

        Assert.Equal("024", stringWriter.ToString());
    }

    [Fact]
    public void NonGenericProperty()
    {
        var assembly = Compile("""
            import System.Collections;
            import System.Console;

            public func main() {
                Write(ArrayList().Count);
            }
            """);
        var stringWriter = new StringWriter();
        _ = Invoke<object?>(
            assembly: assembly,
            methodName: "main",
            stdin: null,
            stdout: stringWriter);

        Assert.Equal("0", stringWriter.ToString());
    }

    [Fact]
    public void EnumeratingRange()
    {
        var assembly = Compile("""
            import System.Console;
            import System.Linq.Enumerable;

            public func main() {
                val enumerable = Range(0, 10);
                val enumerator = enumerable.GetEnumerator();
                while(enumerator.MoveNext())
                {
                    Write(enumerator.Current);
                }
            }
            """);
        var stringWriter = new StringWriter();
        _ = Invoke<object?>(
            assembly: assembly,
            methodName: "main",
            stdin: null,
            stdout: stringWriter);

        Assert.Equal("0123456789", stringWriter.ToString());
    }

    [Fact]
    public void EnumeratingList()
    {
        var assembly = Compile("""
            import System.Console;
            import System.Collections.Generic;

            public func main() {
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
        _ = Invoke<object?>(
            assembly: assembly,
            methodName: "main",
            stdin: null,
            stdout: stringWriter);

        Assert.Equal("235", stringWriter.ToString());
    }

    [Fact]
    public void StringifyInt()
    {
        var assembly = Compile("""
            public func stringify(n: int32): string = n.ToString();
            """);
        var result = Invoke<string>(
            assembly: assembly,
            methodName: "stringify",
            args: 123);

        Assert.Equal("123", result);
    }

    [Fact]
    public void ParseInt()
    {
        var assembly = Compile("""
            public func parse(n: string): int32 = int32.Parse(n);
            """);
        var result = Invoke<int>(
            assembly: assembly,
            methodName: "parse",
            args: "123");

        Assert.Equal(123, result);
    }
}
