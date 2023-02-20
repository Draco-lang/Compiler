using Draco.Fuzzer.Testing;
using Draco.Fuzzer.Testing.Generators;

namespace Draco.Fuzzer;

internal class Program
{
    private static void Main(string[] args)
    {
        var tester = new ParserFuzzer(new RandomValidTokenGenerator());
        tester.StartTesting(50000, 0);
    }
}
