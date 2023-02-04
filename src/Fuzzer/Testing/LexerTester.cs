using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Syntax;
using Fuzzer.Testing.Generators;

namespace Fuzzer.Testing;

internal class LexerTester : IComponentTester
{
    public void StartTesting(int numEpoch, int numMutations)
    {
        RandomInputGenerator generator = new RandomInputGenerator();
        for (int i = 0; i < numEpoch; i++)
        {
            var currentInptut = generator.NextExpoch();
            var lexer = new Lexer(SourceReader.From(currentInptut));
            this.LexAll(lexer, currentInptut);
            for (int j = 0; j < numMutations; j++)
            {
                throw new NotImplementedException();
            }
        }
    }

    private void LexAll(Lexer lexer, string input)
    {
        try
        {
            while (true)
            {
                var token = lexer.Lex();
                if (token.Type == TokenType.EndOfInput) break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(input);
            Console.WriteLine();
            Console.WriteLine(ex.Message);
            Console.WriteLine();
            Console.WriteLine(ex.StackTrace);
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(new string('-', 80));
            Console.ForegroundColor = color;
        }
    }
}
