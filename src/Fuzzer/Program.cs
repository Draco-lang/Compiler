using Fuzzer.Testing;

namespace Fuzzer;

internal class Program
{
    static void Main(string[] args)
    {
        LexerTester tester = new LexerTester();
        tester.StartTesting(5000, 0);
    }
}
