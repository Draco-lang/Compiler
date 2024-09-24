using System;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Terminal.Gui;

namespace Draco.Compiler.Fuzzer;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        Application.Init();
        var debuggerWindow = new TuiTracer();

        var fuzzer = FuzzerFactory.CreateOutOfProcess(debuggerWindow);

        fuzzer.Enqueue(SyntaxTree.Parse("""
            func main() {}
            func foo() {}
            func bar() {}
            func baz() {}
            func qux() {}
            """));
        fuzzer.Enqueue(SyntaxTree.Parse("""
            import System.Console;

            func main() {
                WriteLine("Hello, world!");
            }
            """));
        fuzzer.Enqueue(SyntaxTree.Parse("""
            import System.Console;
            import System.Linq.Enumerable;

            func fib(n: int32): int32 =
                if (n < 2) 1
                else fib(n - 1) + fib(n - 2);

            func main() {
                for (i in Range(0, 10)) {
                    WriteLine("fib(\{i}) = \{fib(i)}");
                }
            }
            """));

        var fuzzerTask = Task.Run(() => fuzzer.Fuzz(CancellationToken.None));

        Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(500), loop =>
        {
            Application.Refresh();
            return true;
        });

        Application.Run(Application.Top);
        await fuzzerTask;
        Application.Shutdown();
    }
}
