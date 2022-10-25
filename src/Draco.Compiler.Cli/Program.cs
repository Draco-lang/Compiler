using System;
using Draco.Compiler.Api.Scripting;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Cli;

internal class Program
{
    internal static void Main(string[] args)
    {
        ScriptingEngine.Execute("""
            func abs(n: int32) =
                if (n < 0) -n
                else n;

            func main() {
                println(abs(123));
            }
            """);
    }
}
