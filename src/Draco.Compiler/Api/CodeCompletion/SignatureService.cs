using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion;

public static class SignatureService
{
    public static SignatureCollection GetSignature(SyntaxTree tree, SemanticModel semanticModel, SyntaxPosition cursor)
    {
        var call = tree.Root.TraverseSubtreesAtCursorPosition(cursor).LastOrDefault(x => x is CallExpressionSyntax) as CallExpressionSyntax;
        if (call is null) return new SignatureCollection(ImmutableArray<SignatureItem>.Empty, 0, null);
        var symbols = semanticModel.GetReferencedOverloads(call.Function).Select(x => (FunctionSymbol)x).OrderBy(x => x.Parameters.Length).ToList();
        var paramCount = call.ArgumentList.Values.Count();
        var separatorCount = call.ArgumentList.Separators.Count();
        var activeParam = separatorCount == paramCount - 1 ? paramCount - 1 : paramCount;
        var matchingOverload = symbols.FirstOrDefault(x => x.Parameters.Length == paramCount && (separatorCount == paramCount - 1 || paramCount == 0));
        if (matchingOverload is null) matchingOverload = symbols.FirstOrDefault(x => x.Parameters.Length > paramCount);
        var activeOverload = matchingOverload is null ? 0 : symbols.IndexOf(matchingOverload);
        var result = ImmutableArray.CreateBuilder<SignatureItem>();
        for (int i = 0; i < symbols.Count; i++)
        {
            result.Add(new SignatureItem($"func {symbols[i].Name}({string.Join(", ", symbols[i].Parameters.Select(x => $"{x.Name}: {x.Type}"))}): {symbols[i].ReturnType}",
                symbols[i].Documentation, symbols[i].Parameters.Select(x => x.Name).ToImmutableArray()));
        }
        return new SignatureCollection(result.ToImmutable(), activeOverload, activeParam == -1 ? null : activeParam);
    }
}
