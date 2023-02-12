using Draco.Compiler.Api.Syntax;
using Draco.Fuzzer.Testing.Generators;

namespace Draco.Fuzzer.Testing;

internal class ParserFuzzer : ComponentTester
{
    private IInputGenerator generator;
    public ParserFuzzer(FuzzType fuzzType)
    {
        switch (fuzzType)
        {
        case FuzzType.RandomText: this.generator = new RandomTextGenerator(); break;
        default: throw new NotImplementedException();
        }
    }

    public override void RunEpoch()
    {
        var input = this.generator.NextExpoch();
        try
        {
            ParseTree.Parse(input);
        }
        catch (Exception ex)
        {
            Helper.PrintError(ex, input);
        }
    }
    public override void RunMutation() => throw new NotImplementedException();
}
