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
                Console.WriteLine("Hi!");
            }
            """);
        var call = tree.Children.First().Children.ElementAt(4).Children.First().Children.ElementAt(1).Children.First();
        Console.WriteLine(call.Range);
    }
}
