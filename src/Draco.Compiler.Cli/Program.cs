using System;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Cli;

internal class Program
{
    internal static void Main(string[] args)
    {
        var ast = ParseTree.Parse("""
            func main() {
            """);
        Console.WriteLine(ast.ToDebugString());
    }
}
