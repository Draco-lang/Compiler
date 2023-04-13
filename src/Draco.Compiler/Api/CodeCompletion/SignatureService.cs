using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

public static class SignatureService
{
    public static ImmutableArray<SignatureItem>? GetSignature(SyntaxTree tree, SemanticModel semanticModel, SyntaxPosition cursor)
    {
        var call = tree.Root.TraverseSubtreesAtCursorPosition(cursor).LastOrDefault(x => x is CallExpressionSyntax) as CallExpressionSyntax;
        if (call is null) return null;
        var symbols = semanticModel.GetReferencedOverloads(call.Function).Select(x => (FunctionSymbol)x);
        var result = ImmutableArray.CreateBuilder<SignatureItem>();
        foreach (var overload in symbols)
        {
            result.Add(new SignatureItem($"func {overload.Name}({string.Join(", ", overload.Parameters.Select(x => $"{x.Name}: {x.Type}"))}): {overload.ReturnType}",
                overload.Documentation, overload.Parameters.Select(x => x.Name).ToImmutableArray()));
        }
        return result.ToImmutable();
    }
}
