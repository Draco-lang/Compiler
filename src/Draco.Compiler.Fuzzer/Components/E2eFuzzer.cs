using Draco.Compiler.Api;
using Draco.Compiler.Api.Scripting;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Fuzzer.Generators;

namespace Draco.Compiler.Fuzzer.Components;

/// <summary>
/// Fuzzes the compiler end-to-end.
/// </summary>
internal sealed class E2eFuzzer(IGenerator<string> inputGenerator)
    : ComponentFuzzerBase<string>(inputGenerator)
{
    protected override void NextEpochInternal(string input)
    {
        var sourceText = SourceText.FromText(input);
        var parseTree = SyntaxTree.Parse(sourceText);
        var compilation = Compilation.Create(
            syntaxTrees: [parseTree]);
        _ = Script.ExecuteAsProgram(compilation);
    }

    protected override void NextMutationInternal(string oldInput, string newInput) =>
        throw new NotImplementedException();
}
