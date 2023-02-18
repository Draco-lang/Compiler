using Draco.Fuzzer.Testing;
using Draco.Fuzzer.Testing.Generators;

namespace Draco.Fuzzer;

internal class Program
{
    private static void Main(string[] args)
    {
        var tester = new CompilerFuzzer(new RandomTextGenerator());
        tester.StartTesting(50, 0);
    }
}
