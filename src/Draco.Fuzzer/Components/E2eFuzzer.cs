using System.Collections.Immutable;
using Draco.Compiler.Api.Scripting;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Api;
using Draco.Fuzzer.Generators;

namespace Draco.Fuzzer.Components;

/// <summary>
/// Fuzzes the compiler end-to-end.
/// </summary>
internal sealed class E2eFuzzer : ComponentFuzzerBase<string>
{
    public E2eFuzzer(IGenerator<string> inputGenerator)
        : base(inputGenerator)
    {
    }

    protected override void NextEpochInternal(string input)
    {
        var sourceText = SourceText.FromText(input);
        var parseTree = SyntaxTree.Parse(sourceText);
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(parseTree));
        _ = ScriptingEngine.Execute(compilation);
    }

    protected override void NextMutationInternal(string oldInput, string newInput) =>
        throw new NotImplementedException();
}
