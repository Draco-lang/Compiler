using System;
using Draco.Compiler.Api.Scripting;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Cli;

internal class Program
{
    internal static void Main(string[] args)
    {
        ScriptingEngine.Execute("""
            func main() {
                hehe();
            }
            """);
    }
}
