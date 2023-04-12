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
        var symbol = semanticModel.GetReferencedSymbol(call.Function);
        if (symbol is null) return null;
        return ImmutableArray.Create<SignatureItem>(new SignatureItem("", ""));
    }
}
