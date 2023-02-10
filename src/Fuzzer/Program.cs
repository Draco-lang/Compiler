using Draco.Fuzzer.Testing;

namespace Draco.Fuzzer;

internal class Program
{
    private static void Main(string[] args)
    {
        var tester = new ParserFuzzer();
        tester.StartTesting(50000, 0);
    }
}
