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
        var _ = Invoke<object?>(
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
        var _ = Invoke<object?>(
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
        var _ = Invoke<object?>(
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
        var _ = Invoke<object?>(
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
        var _ = Invoke<object?>(
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
        var _ = Invoke<object?>(
            assembly: assembly,
            methodName: "main",
            stdin: null,
            stdout: stringWriter);

        Assert.Equal("321", stringWriter.ToString());
    }
}
