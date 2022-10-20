using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Draco.RedGreenTree.SourceGeneration;

internal static class IncrementalGeneratorInitializationContextExtensions
{
    public static IncrementalValuesProvider<INamedTypeSymbol> ForTypesWithGreenTreeAttribute(this IncrementalGeneratorInitializationContext ctx) =>
        ctx.SyntaxProvider.ForAttributeWithMetadataName(
            "Draco.RedGreenTree.GreenTreeAttribute",
            (syntaxNode, ct) => syntaxNode is TypeDeclarationSyntax,
            (ctx, ct) => (INamedTypeSymbol)ctx.TargetSymbol);
}
