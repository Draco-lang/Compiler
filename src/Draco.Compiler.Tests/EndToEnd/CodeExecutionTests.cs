using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Draco.Compiler.Tests.TestUtilities;

namespace Draco.Compiler.Tests.EndToEnd;

[Collection(nameof(NoParallelizationCollectionDefinition))]
public class CodeExecutionTests
{
    [Fact]
    public void ClassHelloWorld()
    {
        var assembly = CompileToAssembly("""
            import System.Console;

            func main() {
                WriteLine("Hello, World!");
            }

            class Foo {
                func bar() {
                    WriteLine("Hello, World!");
                }
            }
            """);

        var stringWriter = new StringWriter();
        _ = Invoke<object?>(assembly: assembly, stdout: stringWriter);

        Assert.Equal($"Hello, World!{Environment.NewLine}", stringWriter.ToString(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void InstanceField()
    {
        var assembly = CompileToAssembly("""
        import System.Console;
        
        func main() {
            var foo = Foo();
        }
        
        class Foo {
            field var i: int;
            func increment(this) {
                this.i += 1;
            }
        }

        """);
    }
}
