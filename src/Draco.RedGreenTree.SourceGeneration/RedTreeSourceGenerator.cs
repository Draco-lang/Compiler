using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Draco.RedGreenTree.SourceGeneration;

[Generator]
public sealed class RedTreeSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var trees = context.SyntaxProvider.ForAttributeWithMetadataName(
            "Draco.RedGreenTree.GreenTreeAttribute",
            (syntaxNode, ct) => syntaxNode is TypeDeclarationSyntax,
            (ctx, ct) => (INamedTypeSymbol)ctx.TargetSymbol);

        context.RegisterSourceOutput(trees, (ctx, symbol) =>
        {
            var source = RedTreeGenerator.Generate(symbol);

            var redTypeName = RedTreeGenerator.GetRedClassName(symbol);
            var name = $"{redTypeName}.g.cs";

            ctx.AddSource(name, source);
        });
    }
}
