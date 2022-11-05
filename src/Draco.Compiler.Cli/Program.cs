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
                var x = ReadLine();
                var a: int32 = );
                if (x == "0") {
                    Write("0");
                }
                else {
                    while (true) Write("1");
                }
            }
            """);
        foreach (var diag in tree.GetAllDiagnostics())
        {
            Console.WriteLine(diag);
        }
    }
}
