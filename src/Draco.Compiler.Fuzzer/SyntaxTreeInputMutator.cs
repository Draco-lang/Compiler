using System;
using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Fuzzing;

namespace Draco.Compiler.Fuzzer;

internal sealed class SyntaxTreeInputMutator : IInputMutator<SyntaxTree>
{
    public IEnumerable<SyntaxTree> Mutate(Random random, SyntaxTree input)
    {
        // For now we do something very simple and just swap around tokens
        var swapper = InputMutator.Swap<SyntaxToken>();
        var tokens = input.Root.Tokens.ToList();
        foreach (var mutantList in swapper.Mutate(random, tokens).Take(30))
        {
            var source = string.Join(" ", mutantList.Select(t => t.Text));
            yield return SyntaxTree.Parse(source);
        }
    }
}
