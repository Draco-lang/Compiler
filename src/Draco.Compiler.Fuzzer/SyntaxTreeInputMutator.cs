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
        // For now we do something very simple and just swap around and remove tokens
        var swapper = InputMutator.Swap<SyntaxToken>();
        var remover = InputMutator.Remove<SyntaxToken>();
        var tokens = input.Root.Tokens.ToList();
        // The last token is the EOF token, we don't want to swap that
        tokens.RemoveAt(tokens.Count - 1);
        foreach (var mutantList in swapper.Mutate(random, tokens).Take(5))
        {
            var source = string.Join(" ", mutantList.Select(t => t.Text));
            yield return SyntaxTree.Parse(source);
        }
        // We also remove tokens
        foreach (var mutantList in remover.Mutate(random, tokens).Take(5))
        {
            var source = string.Join(" ", mutantList.Select(t => t.Text));
            yield return SyntaxTree.Parse(source);
        }
    }
}
