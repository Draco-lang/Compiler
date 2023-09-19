using BenchmarkDotNet.Running;

namespace Draco.Compiler.Benchmarks;

internal class Program
{
    internal static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
