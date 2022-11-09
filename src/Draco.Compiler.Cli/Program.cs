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
                var a = 0;
                var b = a + 1;
            }
            """";
        ScriptingEngine.Execute(src);
    }
}
