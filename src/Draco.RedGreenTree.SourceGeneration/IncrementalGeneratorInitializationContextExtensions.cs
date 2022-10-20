using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Draco.RedGreenTree.SourceGeneration;

internal static class IncrementalGeneratorInitializationContextExtensions
{
    public static IncrementalValuesProvider<AttributeAndType> ForTypesWithAttribute(this IncrementalGeneratorInitializationContext ctx, string attributeName) =>
        ctx.SyntaxProvider.ForAttributeWithMetadataName(
            attributeName,
            (syntaxNode, ct) => syntaxNode is TypeDeclarationSyntax,
            AttributeAndType? (ctx, ct) =>
            {
                if (ctx.TargetSymbol is not INamedTypeSymbol namedTypeSymbol) return null;

                var attribute = ctx.Attributes.First(
                    atr => atr.AttributeClass?.GetNamespacedName() == attributeName);

                return new(attribute, namedTypeSymbol);
            })
            .NotNull();

    public readonly record struct AttributeAndType(AttributeData Attribute, INamedTypeSymbol Type);
}
