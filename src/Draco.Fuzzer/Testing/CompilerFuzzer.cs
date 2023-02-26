using Draco.Compiler.Api.Scripting;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Api;
using Draco.Fuzzer.Testing.Generators;

namespace Draco.Fuzzer.Testing;

internal sealed class CompilerFuzzer : ComponentFuzzer
{
    private IInputGenerator<string> generator;

    public CompilerFuzzer(IInputGenerator<string> generator)
    {
        this.generator = generator;
    }

    public override void RunEpoch()
    {
        var input = this.generator.NextExpoch();
        try
        {
            var sourceText = SourceText.FromText(input);
            var parseTree = SyntaxTree.Parse(sourceText);
            var compilation = Compilation.Create(parseTree);
            var execResult = ScriptingEngine.Execute(compilation);
        }
        catch (Exception ex)
        {
            this.AddError(ex, input);
        }
    }

    public override void RunMutation() => throw new NotImplementedException();
}
