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
            func main() {
                var x;
                var y;
                {
                    var x;
                    var z = x + w;
                }
            }
            """";
        var compilation = new Compilation();
        var parseTree = ParseTree.Parse(src);
        var hasErrors = false;
        foreach (var diag in parseTree.GetAllDiagnostics())
        {
            hasErrors = true;
            Console.WriteLine(diag);
        }
        if (hasErrors) return;
        var semanticModel = compilation.GetSemanticModel(parseTree);
        foreach (var diag in semanticModel.GetAllDiagnostics())
        {
            hasErrors = true;
            Console.WriteLine(diag);
        }
    }
}
