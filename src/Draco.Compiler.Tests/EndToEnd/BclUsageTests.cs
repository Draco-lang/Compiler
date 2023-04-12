using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Tests.EndToEnd;

public sealed class BclUsageTests : EndToEndTestsBase
{
    [Fact]
    public void HelloWorld()
    {
        var assembly = Compile("""
            import System.Console;

            func main() {
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
    public void SimpleInterpolation()
    {
        var assembly = Compile("""
            import System.Console;

            func main() {
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
}
