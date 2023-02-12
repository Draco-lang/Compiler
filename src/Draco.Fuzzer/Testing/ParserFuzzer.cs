using Draco.Compiler.Api.Syntax;
using Draco.Fuzzer.Testing;
using Draco.Fuzzer.Testing.Generators;

namespace Draco.Fuzzer.Testing;

internal class ParserFuzzer : ComponentTester
{
    private RandomInputGenerator generator;
    public ParserFuzzer()
    {
        this.generator = new RandomInputGenerator();
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
