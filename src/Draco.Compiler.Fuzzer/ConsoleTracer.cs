using System;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Coverage;
using Draco.Fuzzing;

namespace Draco.Compiler.Fuzzer;

internal sealed class ConsoleTracer : ITracer<SyntaxTree>
{
    public void EndOfMinimization(SyntaxTree input, SyntaxTree minimizedInput, CoverageResult coverage, TimeSpan elapsed)
    {
        Console.WriteLine("==== MINIMZATION OVER ====");
        Console.WriteLine($"Original: {input}");
        Console.WriteLine($"Minimized: {minimizedInput}");
        Console.WriteLine($"Coverage: {CoverageToPercentage(coverage) * 100}%");
        Console.WriteLine($"Elapsed: {elapsed}");
        Console.WriteLine("==========================");
    }

    public void EndOfMutations(SyntaxTree input, int mutationsFound, TimeSpan elapsed)
    {
        Console.WriteLine("==== MUTATIONS OVER ====");
        Console.WriteLine($"New mutations: {mutationsFound}");
        Console.WriteLine($"Elapsed: {elapsed}");
        Console.WriteLine("========================");
    }

    public void InputFaulted(SyntaxTree input, FaultResult fault)
    {
        Console.WriteLine("==== FAULT DETECTED ====");
        Console.WriteLine($"Input: {input}");
        Console.WriteLine($"Fault: {fault}");
        Console.WriteLine("========================");
    }

    private static double CoverageToPercentage(CoverageResult coverage) =>
        coverage.Entires.Count(e => e.Hits > 0) / (double)coverage.Entires.Length;
}
