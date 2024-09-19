using System.Reflection;
using Draco.Compiler.Api;
using Draco.Coverage;

namespace Sandbox;

internal class Program
{
    static void Main(string[] args)
    {
        var asm = InstrumentedAssembly.FromWeavedAssembly(typeof(Compilation).Assembly);
        asm.ClearCoverageData();

        var compilation = Compilation.Create(
            syntaxTrees: []);

        var result = asm.GetCoverageResult();
        Console.WriteLine(result.Entires.Count(e => e.Hits > 0));
    }
}
