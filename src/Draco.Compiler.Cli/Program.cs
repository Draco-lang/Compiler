using System;
using System.Linq;
using Draco.Compiler.Api.Scripting;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Cli;

internal class Program
{
    internal static void Main(string[] args)
    {
        var tree = ParseTree.Parse("""
            func main() {
                var a = 0
                Console.WriteLine("Hi!");
            }
            """);
        foreach (var diag in tree.GetAllDiagnostics())
        {
            Console.WriteLine(diag);
        }
    }
}
