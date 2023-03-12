using System.CommandLine;
using Draco.Fuzzer.Testing;
using Draco.Fuzzer.Testing.Generators;

namespace Draco.Fuzzer;

internal static class Program
{
    internal static int Main(string[] args) => ConfigureCommands().Invoke(args);

    private static RootCommand ConfigureCommands()
    {
        var numEpochsOption = new Option<int>(new string[] { "-e", "--epochs" }, () => -1, description: "Specifies the number of epochs the fuzzer should run for, if not specified or -1, the fuzzer will run indefinitely");
        var numMutationsOption = new Option<int>(new string[] { "-m", "--mutations" }, () => 0, description: "Specifies the number of mutations the fuzzer should make for each epoch, if not specified or 0, there will be no mutations");

        var lexerCommand = new Command("lexer", "Fuzzes the lexer")
        {
            numEpochsOption,
            numMutationsOption,
        };
        lexerCommand.SetHandler(FuzzLexer, numEpochsOption, numMutationsOption);

        var parserCommand = new Command("parser", "Fuzzes the parser")
        {
            numEpochsOption,
            numMutationsOption,
        };
        parserCommand.SetHandler(FuzzParser, numEpochsOption, numMutationsOption);

        var compilerCommand = new Command("compiler", "Fuzzes the compiler")
        {
            numEpochsOption,
            numMutationsOption,
        };
        compilerCommand.SetHandler(FuzzCompiler, numEpochsOption, numMutationsOption);

        var rootCommand = new RootCommand("CLI for the Draco fuzzer");
        rootCommand.AddCommand(lexerCommand);
        rootCommand.AddCommand(parserCommand);
        rootCommand.AddCommand(compilerCommand);
        return rootCommand;
    }

    private static void FuzzLexer(int numEpochs, int numMutations) => new LexerFuzzer(new RandomTextGenerator()).StartTesting(numEpochs, numMutations);

    private static void FuzzParser(int numEpochs, int numMutations) => new ParserFuzzer(new RandomValidTokenGenerator()).StartTesting(numEpochs, numMutations);

    private static void FuzzCompiler(int numEpochs, int numMutations) => new CompilerFuzzer(new RandomTextGenerator()).StartTesting(numEpochs, numMutations);
}
