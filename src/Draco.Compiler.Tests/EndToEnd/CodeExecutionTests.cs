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
        
        func main(): int32 {
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

        var stringWriter = new StringWriter();
        var value = Invoke<int>(assembly: assembly, stdout: stringWriter);
        Assert.Equal(1, value);

    }
}
