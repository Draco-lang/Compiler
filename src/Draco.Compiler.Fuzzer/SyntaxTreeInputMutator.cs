using System;
using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Api.Syntax.Extensions;
using Draco.Fuzzing;

namespace Draco.Compiler.Fuzzer;

internal sealed class SyntaxTreeInputMutator : IInputMutator<SyntaxTree>
{
    public IEnumerable<SyntaxTree> Mutate(Random random, SyntaxTree input)
    {
        // For now we only mutate tokens
        var swapper = InputMutator.Swap<SyntaxToken>();
        var remover = InputMutator.Remove<SyntaxToken>();
        var splicer = InputMutator.Splice<SyntaxToken>();
        var tokens = input.Root.Tokens.ToList();
        // The last token is the EOF token, we don't want to use that
        tokens.RemoveAt(tokens.Count - 1);
        // We try to swap tokens
        foreach (var mutantList in swapper.Mutate(random, tokens).Take(5)) yield return TokenListToSyntaxList(mutantList);
        // We also remove tokens
        foreach (var mutantList in remover.Mutate(random, tokens).Take(5)) yield return TokenListToSyntaxList(mutantList);
        // We also try to splice tokens
        foreach (var mutantList in splicer.Mutate(random, tokens).Take(5)) yield return TokenListToSyntaxList(mutantList);
    }

    private static SyntaxTree TokenListToSyntaxList(IEnumerable<SyntaxToken> tokens) =>
        SyntaxTree.Parse(string.Join(" ", tokens.Select(t => t.Text))).Format();
}
