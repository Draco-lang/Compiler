using System;
using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Scripting;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Cli;

internal class Program
{
    internal static void Main(string[] args)
    {
        var src = """"
            func main(): int32 {
                var x;
                var y;
                {
                    var x;
                    var z = x + y;
                }
            }
            """";
        var compilation = new Compilation();
        var parseTree = ParseTree.Parse(src);
        var semanticModel = compilation.GetSemanticModel(parseTree);
        Console.WriteLine(semanticModel.ToScopeTreeDotGraphString());
        //Console.WriteLine(parseTree.ToDotGraphString());
    }
}
