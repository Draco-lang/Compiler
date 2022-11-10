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
            func main(): int32 {
                println(between(1, 0, 10));
                println(between(5, 0, 10));
                println(between(12, 0, 10));
            }

            func between(x: int32, a: int32, b: int32): bool =
                a < x < b;
            """";
        ScriptingEngine.Execute(src);
    }
}
