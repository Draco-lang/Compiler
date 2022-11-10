using System;
using System.Linq;
using Draco.Compiler.Api.Scripting;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Cli;

internal class Program
{
    internal static void Main(string[] args)
    {
        var src = """"
            func fib(n: int32): int32 =
                if (n < 3) 1
                else fib(n - 1) + fib(n - 2);

            func between(x: int32, a: int32, b: int32): bool =
                a < x < b;

            func main(): int32 {
                println(between(1, 0, 10));
                println(between(5, 0, 10));
                println(between(12, 0, 10));
                println(fib(6));
            }
            """";
        ScriptingEngine.Execute(src);
    }
}
