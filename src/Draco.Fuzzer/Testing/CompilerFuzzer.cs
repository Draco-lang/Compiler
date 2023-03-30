using Draco.Compiler.Api.Scripting;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Api;
using Draco.Fuzzer.Testing.Generators;
using System.Collections.Immutable;

namespace Draco.Fuzzer.Testing;

internal sealed class CompilerFuzzer : ComponentFuzzer<string>
{
    public CompilerFuzzer(IInputGenerator<string> generator) : base(generator) { }

    public override void RunEpoch(string input)
    {
        var sourceText = SourceText.FromText(input);
        var parseTree = SyntaxTree.Parse(sourceText);
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(parseTree));
        var execResult = ScriptingEngine.Execute(compilation);
    }

    public override void RunMutation() => throw new NotImplementedException();
}
