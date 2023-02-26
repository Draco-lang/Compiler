using Draco.Compiler.Internal.Syntax;
using Draco.Fuzzer.Testing.Generators;

namespace Draco.Fuzzer.Testing;

internal sealed class ParserFuzzer : ComponentFuzzer
{
    private IInputGenerator<IEnumerable<SyntaxToken>> generator;

    public ParserFuzzer(IInputGenerator<IEnumerable<SyntaxToken>> generator)
    {
        this.generator = generator;
    }

    public override void RunEpoch()
    {
        var input = this.generator.NextExpoch();
        try
        {
            // We just care about the parsing into compilation unit part
            new Parser(SyntaxTokenSource.From(input)).ParseCompilationUnit();
        }
        catch (Exception ex)
        {
            this.AddError(ex, string.Join("", input.Select(x => x.Text)));
        }
    }

    public override void RunMutation() => throw new NotImplementedException();
}
