using System;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Coverage;
using Draco.Fuzzing;

namespace Draco.Compiler.Fuzzer;

internal static class Program
{
    private static void Main(string[] args)
    {
        var fuzzer = new Fuzzer<SyntaxTree, int>
        {
            InstrumentedAssembly = InstrumentedAssembly.FromWeavedAssembly(typeof(Compilation).Assembly),
            CoverageCompressor = CoverageCompressor.Hash,
            FaultDetector = FaultDetector.Default(TimeSpan.FromSeconds(5)),
            TargetExecutor = TargetExecutor.Create((SyntaxTree tree) => RunCompilation(tree)),
            InputMinimizer = TODO,
            InputMutator = TODO,
            Tracer = TODO,
        };
    }

    private static void RunCompilation(SyntaxTree syntaxTree)
    {
        // TODO
    }
}
