using Draco.Compiler.Internal.Syntax;
using Draco.Fuzzer.Testing.Generators;

namespace Draco.Fuzzer.Testing;

internal sealed class ParserFuzzer : ComponentFuzzer
{
    private IInputGenerator<ParseNode.Token[]> generator;

    public ParserFuzzer(IInputGenerator<ParseNode.Token[]> generator)
    {
        this.generator = generator;
    }

    public override void RunEpoch()
    {
        var input = this.generator.NextExpoch();
        try
        {
            // We just care about the parsing into compilation unit part
            new Parser(TokenSource.From(input)).ParseCompilationUnit();
        }
        catch (Exception ex)
        {
            Helper.PrintError(ex, string.Join(Environment.NewLine, (IEnumerable<ParseNode.Token>)input));
        }
    }
    public override void RunMutation() => throw new NotImplementedException();
}
