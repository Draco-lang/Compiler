using System.Collections.Generic;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Api.Syntax.Extensions;
using Draco.Fuzzing;

namespace Draco.Compiler.Fuzzer;

internal sealed class SyntaxTreeInputMinimizer : IInputMinimizer<SyntaxTree>
{
    public IEnumerable<SyntaxTree> Minimize(SyntaxTree input)
    {
        // We try to throw away nodes one by one
        foreach (var node in input.Root.PreOrderTraverse())
        {
            if (!CanBeThrownAway(node)) continue;
            yield return input.Remove(node);
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
