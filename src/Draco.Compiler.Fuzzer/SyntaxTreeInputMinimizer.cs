using System;
using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Api.Syntax.Extensions;
using Draco.Fuzzing;

namespace Draco.Compiler.Fuzzer;

internal sealed class SyntaxTreeInputMinimizer : IInputMinimizer<SyntaxTree>
{
    public IEnumerable<SyntaxTree> Minimize(Random random, SyntaxTree input)
    {
        // Get the nodes that can be thrown away
        var targets = input.Root
            .PreOrderTraverse()
            .Where(CanBeThrownAway)
            .ToList();
        if (targets.Count == 0) yield break;

        // Just try a few random samples
        for (var i = 0; i < 15; i++)
        {
            var target = targets[random.Next(targets.Count)];
            yield return input.Remove(target);
        }
    }

    private static bool CanBeThrownAway(SyntaxNode node)
    {
        // We only throw away nodes that are within SyntaxList<T>s
        if (node.Parent is null) return false;
        var parentType = node.Parent.GetType();
        if (!parentType.IsGenericType) return false;
        return parentType.GetGenericTypeDefinition() == typeof(SyntaxList<>);
    }
}
